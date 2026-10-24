"""Author finite-position variants while retaining the construction ladders.

This is an offline calibration experiment. The supplied source premises choose
what coarsening must preserve. Native replay consumes only each resulting
connected program; it never requests the fine original to complete a picture.
"""
from __future__ import annotations

import argparse
import json
from math import dist
from pathlib import Path

from authored_line_art import alphabet_scene
from construction_alphabet import LETTERS, _construct
from construction_churches import MASS_PLANS, _build_church, church_sources
from construction_engines import source_digest
from construction_recipes import loc
from verify_construction_ladders import evaluate


LEVELS = [
    {"id": "exact", "label": "Measured", "divisions": 0,
     "description": "Original measured placement, with explicit shared alignment and mirror reads."},
    *[{"id": f"q{n}", "label": f"{n} divisions", "divisions": n,
       "description": "Requested finite placement grid; finer exceptions protect the selected relationships."}
      for n in (64, 32, 16, 8)],
]


def _control_sites(program):
    edges = {e["id"]: e for e in program["edges"]}
    return {key: edges[value]["controls"]
            for key, value in program["sourceMapping"]["edgeMap"].items()}


def connect_preserved_reads(program, original, coarse):
    """Make observed alignment/reflection actual references, without site welding.

    x_mirror = axis + (axis - earlier_x); y_alignment = earlier_y.
    These are ordinary native affine relations, not new operation labels.
    Existing translated occurrences retain their original control origins.
    Only a later coordinate can consume an earlier coordinate.
    """
    controls = _control_sites(program)
    expected, _, _ = evaluate(program)
    original_edges = {e["id"]: e for e in original["edges"]}
    original_values = {}
    for key, ids in controls.items():
        for site, point in zip(ids, original_edges[key]["controls"]):
            if site in original_values and original_values[site] != point:
                raise ValueError("A shared control identity has inconsistent source coordinates")
            original_values[site] = point
    protected = {site for repeat in program["repeats"] for occurrence in repeat["instances"][1:]
                 for edge_id in occurrence
                 for site in next(e for e in program["edges"] if e["id"] == edge_id)["controls"]}
    # Publish previously descriptive alphabet repetitions as actual causal
    # translated occurrences whenever the existing drawing order permits them.
    ordinal = {s["id"]: i for i, s in enumerate(program["sites"])}
    edge_rows = {e["id"]: e for e in program["edges"]}
    translations = {}
    existing = {r["id"] for r in program["repeats"]}
    for index, claim in enumerate(coarse.get("correspondences", [])):
        if claim["kind"] != "repeat" or claim["id"] in existing:
            continue
        if not set(claim["source"] + claim["target"]) <= controls.keys() or any(claim["reversed"]):
            continue
        pairs = [(a, b) for source, target in zip(claim["source"], claim["target"])
                 for a, b in zip(controls[source], controls[target])]
        if any(ordinal[a] >= ordinal[b] or b in translations for a, b in pairs):
            continue
        dx, dy = claim["translation"]
        for a, b in pairs:
            translations[b] = (a, dx, dy)
            protected.add(b)
        source_edges = [program["sourceMapping"]["edgeMap"][e] for e in claim["source"]]
        target_edges = [program["sourceMapping"]["edgeMap"][e] for e in claim["target"]]
        program["repeats"].append({"id": "resolution-" + claim["id"],
            "at": max(edge_rows[e]["at"] for e in source_edges + target_edges),
            "source": source_edges, "instances": [source_edges, target_edges]})
    mirrors = {}
    anchors = []
    page_x, _, width, height = program["viewBox"]
    for index, claim in enumerate(coarse.get("correspondences", [])):
        if claim["kind"] != "mirror":
            continue
        pairs = [(a, b, reverse) for a, b, reverse in
                 zip(claim["source"], claim["target"], claim["reversed"])
                 if a in controls and b in controls]
        if not pairs:
            continue
        axis_x = claim["axis"][0][0]
        anchor = f"resolution-mirror-axis-{index}"
        anchors.append({"id": anchor, "at": 0,
                        "x": loc("page-origin", "page-extent", axis_x - page_x, width),
                        "y": loc("page-origin")})
        for first, second, reverse in pairs:
            target = list(reversed(controls[second])) if reverse else controls[second]
            for a, b in zip(controls[first], target):
                mirrors.setdefault(a, []).append((b, anchor))
                mirrors.setdefault(b, []).append((a, anchor))
    # The new axis reads consume the two root frame sites, before any drawing.
    program["sites"][2:2] = anchors
    available = set()
    alignment = [{}, {}]
    bindings = []
    for site in program["sites"]:
        key = site["id"]
        if key in translations:
            peer, dx, dy = translations[key]
            site["x"] = loc("page-origin", "page-extent", dx, width, origin=peer)
            site["y"] = loc("page-origin", "page-extent", dy, height, origin=peer)
            bindings.extend({"site": key, "axis": axis, "source": peer, "read": "translation"}
                            for axis in ("x", "y"))
        if key in original_values:
            for dimension, axis in enumerate(("x", "y")):
                value = original_values[key][dimension]
                if key not in protected:
                    mirror = next(((peer, anchor) for peer, anchor in mirrors.get(key, [])
                                   if peer == key or peer in available), None)
                    if axis == "x" and mirror:
                        peer, anchor = mirror
                        site[axis] = loc(anchor) if peer == key else loc(peer, anchor, 1, 1, origin=anchor)
                        bindings.append({"site": key, "axis": axis, "source": anchor if peer == key else peer,
                                         "axisSite": anchor, "read": "axis" if peer == key else "reflection"})
                    elif value in alignment[dimension]:
                        peer = alignment[dimension][value]
                        site[axis] = loc(peer)
                        bindings.append({"site": key, "axis": axis, "source": peer, "read": "alignment"})
                alignment[dimension].setdefault(value, key)
        available.add(key)
    actual, _, _ = evaluate(program)
    if any(actual[key] != value for key, value in expected.items()):
        raise ValueError("Connected relationship reads changed the chosen finite geometry")
    program["resolutionBindings"] = bindings
    return program


def _rough_plans(scene_id, divisions):
    if not divisions:
        return MASS_PLANS[scene_id]
    quantum = 512 // divisions
    result = []
    for key, kind, bounds in MASS_PLANS[scene_id]:
        left, top, right, bottom = [round(value / quantum) * quantum for value in bounds]
        # These independent templates need positive working spans. They do not
        # constrain the final drawing to lie inside them.
        right, bottom = max(right, left + quantum), max(bottom, top + quantum)
        result.append((key, kind, (left, top, right, bottom)))
    return result


def build(output):
    from construction_resolution_geometry import coarsen_scene

    output.mkdir(parents=True, exist_ok=True)
    programs, entries, scene_reports = [], [], []
    originals = [*church_sources(), alphabet_scene()]
    for level in LEVELS:
        for original in originals:
            coarse, report = coarsen_scene(original, level["divisions"])
            scene_reports.append({"sceneId": original["id"], "levelId": level["id"], **report})
            if original["id"] == "diagnostic-alphabet":
                recipes = [*[_construct(coarse, letter) for letter in LETTERS],
                           _construct(coarse, LETTERS, atlas=True)]
            else:
                recipes = [_build_church(coarse, _rough_plans(original["id"], level["divisions"]))]
            for program in recipes:
                connect_preserved_reads(program, original, coarse)
                base_id = program["id"]
                program["id"] = base_id + "--" + level["id"]
                program["resolution"] = {"baseId": base_id, "levelId": level["id"],
                                         "originalSourceSha256": source_digest(original), **report}
                # Source correspondence describes this finite calibration only.
                # The original exact coordinates remain outside the replay input.
                program["sourceMapping"]["status"] = "finite-calibration-source-correspondence"
                program["description"] = (
                    "The same construction at progressively coarser numeric placement. "
                    "Declared mirror and repeat scopes, counts and relative endpoint order are retained. "
                    "Finer exceptions remain where the requested grid would lose a relationship.")
                programs.append(program)
                source_edges = {e["id"]: e for e in original["edges"]}
                finite_edges = {e["id"]: e for e in coarse["edges"]}
                movements = [dist(a, b) for key in program["sourceMapping"]["sourceEdgeOrder"]
                             for a, b in zip(source_edges[key]["controls"], finite_edges[key]["controls"])]
                entries.append({"id": program["id"], "baseId": base_id, "levelId": level["id"],
                                "label": program["variantLabel"],
                                "retainedFine": report["retainedFineCount"],
                                "independentReads": report["independentReads"],
                                "metricsScope": "source scene", "nominalQuantum": report["nominalQuantum"],
                                "referenceSpan": report["referenceSpan"],
                                "changedCoordinates": sum(distance != 0 for distance in movements),
                                "maxDeviation": max(movements, default=0)})
    manifest = {"schema": "gojo.construction-resolution.v1", "levels": LEVELS,
                "programs": entries, "sceneReports": scene_reports,
                "qualification": "Still numeric, with adaptive finer exceptions. The dial changes placement precision, not drawing-step count. Relationships are authored premises; this is not learned or pre-numeric inference."}
    (output / "programs.ndjson").write_text("".join(json.dumps(p, ensure_ascii=False, separators=(",", ":")) + "\n"
                                                  for p in programs), encoding="utf-8")
    (output / "resolution-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return {"programs": len(programs), "levels": len(LEVELS),
            "rungs": sum(len(p["rungs"]) for p in programs), "output": str(output)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    print(json.dumps(build(parser.parse_args().output), indent=2))


if __name__ == "__main__":
    main()
