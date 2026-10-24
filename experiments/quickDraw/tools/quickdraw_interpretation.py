"""Acquire small church studies and author causal, source-linked constructions.

This is an offline measured adapter. Whole-drawing guides are authored before
the recorded strokes; they are not a claim about the participant's intentions.
Only the native construction executor supplies the review's proposal geometry.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path

from construction_recipes import Recipe, loc
from quickdraw_structure_observations import observe_record, polyline_indices


MODES = (("observed-fit", "Close fit", 1, 0),
         ("gentle-priors", "Gentle interpretation", 4, 12),
         ("stronger-priors", "Stronger interpretation", 12, 24))


def quantize(value, quantum):
    # Deliberate nearest-grid read; unlike native affine replay this is not
    # truncation. Its lost displacement remains in the source/audit pairing.
    return int(math.copysign(math.floor(abs(value)/quantum + 0.5)*quantum, value))


def construct(observation, ordinal, mode, label, quantum, axis_tolerance):
    source_id = observation["sourceId"]
    if observation["bounds"] is None:
        raise ValueError("An empty acquisition has no measured extent for this construction adapter; retain its acquisition without inventing a shape.")
    left, top, right, bottom = observation["bounds"]
    a_xy = [math.floor(left/8)*8, math.floor(top/8)*8]
    b_xy = [max(a_xy[0]+8, math.ceil(right/8)*8),
            max(a_xy[1]+8, math.ceil(bottom/8)*8)]
    width, height = b_xy[0]-a_xy[0], b_xy[1]-a_xy[1]
    r = Recipe(f"quickdraw-{source_id}--{mode}", f"Church {ordinal}", {
        "envelopeLeft": a_xy[0], "envelopeTop": a_xy[1],
        "envelopeRight": b_xy[0], "envelopeBottom": b_xy[1]})
    r.data.update(family="quickdraw-"+source_id, variantLabel=label,
                  description="An offline rough envelope and outline guide the recorded strokes. Measured candidates remain proposals; gaps and source order remain inspectable.")
    mapping = {"sourceId": source_id, "mode": mode, "spanEdges": {},
               "edgeSpans": {}, "sourceStrokes": [], "decisions": {},
               "notes": ["The first two rungs are authored from the whole drawing, not recovered human construction steps.",
                         "Subsequent rungs preserve acquired stroke order; separate pen lifts retain separate sites.",
                         "Cross/oval/corner candidates are measured observations, not mandatory templates or learned probabilities."]}
    r.data["observationMapping"] = mapping
    r.data["sourceMapping"] = {"sourceId": source_id, "spanMap": {},
                               "envelopeSiteIds": ["envelope-a", "envelope-b"]}
    r.data["interpretationPolicy"] = {"quantum": quantum, "axisToleranceDegrees": axis_tolerance,
        "closure": "No inferred closure or contact is imposed.",
        "constructionOrder": "offline envelope, offline outline, acquired stroke order"}
    r.begin("Reserve the drawing's broad extent", "A measured rectangle engine creates a positioning template. It is not an inferred building wall.")
    r.frame()
    a = r.page("envelope-a", "envelopeLeft", "envelopeTop")
    b = r.page("envelope-b", "envelopeRight", "envelopeBottom")
    corners, envelope = r.rectangle("envelope", a, b)
    positions = {a: a_xy, b: b_xy, corners[1]: [b_xy[0], a_xy[1]],
                 corners[3]: [a_xy[0], b_xy[1]]}
    # Acquisition-space positions choose an anchor. Its reconstructed position
    # then carries subsequent deformation instead of cancelling it in offsets.
    reference_positions = dict(positions)
    drawn = list(corners)
    r.finish(envelope)

    def relative(key, origin, displacement, raw_reference):
        dx, dy = displacement
        r.point(key, loc(a, b, dx, width, origin), loc(a, b, dy, height, origin))
        positions[key] = [positions[origin][0]+dx, positions[origin][1]+dy]
        reference_positions[key] = list(raw_reference)
        return key

    def nearest(raw):
        return min(drawn, key=lambda key: sum((raw[j]-reference_positions[key][j])**2 for j in range(2)))

    r.begin("Trace a rough outline", "Long acquired strokes supply coarse connected chords. These are scaffolds, not proven outside boundaries; later strokes replace their own scaffolds.")
    outline = {}
    for stroke in observation["strokes"]:
        if stroke["id"] not in observation["outlineStrokeIds"] or not stroke["points"]:
            continue
        sites = []
        for index in polyline_indices(stroke["points"], 18):
            raw = stroke["points"][index]
            origin = sites[-1] if sites else nearest(raw)
            delta = [quantize(raw[j]-reference_positions[origin][j], 8) for j in range(2)]
            site = relative(f"rough-{stroke['index']}-{index}", origin, delta, raw)
            sites.append(site)
        if len(sites) == 1:
            edges = [r.edge(f"rough-{stroke['index']}-point", [sites[0], sites[0]])]
        else:
            edges = r.poly(f"rough-{stroke['index']}", sites, False)
        outline[stroke["index"]] = edges
        drawn.extend(sites)
    rough = [edge for edges in outline.values() for edge in edges]
    r.finish(rough or envelope, envelope if rough else [])

    by_span = {span["id"]: span for span in observation["spans"]}
    visible, retired, acquired_sites = [], [], {}
    for stroke in observation["strokes"]:
        s = stroke["index"]
        r.begin(f"Add recorded stroke {s+1}", "Place the first site relative to an earlier drawn control, then follow this stroke's ordered intervals. Axis attraction can move later sites and expose disagreements.")
        source_edges = []
        for span_id in stroke["spans"]:
            span = by_span[span_id]
            raw_start, raw_end = span["controls"][0], span["controls"][-1]
            start_key = (s, span["startIndex"])
            if start_key not in acquired_sites:
                origin = nearest(raw_start)
                delta = [quantize(raw_start[j]-reference_positions[origin][j], quantum) for j in range(2)]
                acquired_sites[start_key] = relative(f"sample-{s}-{span['startIndex']}", origin, delta, raw_start)
            start = acquired_sites[start_key]
            raw_delta = [raw_end[j]-raw_start[j] for j in range(2)]
            delta = [quantize(d, quantum) for d in raw_delta]
            attracted = None
            if span["kind"] == "line" and axis_tolerance:
                angle = math.degrees(math.atan2(abs(raw_delta[1]), abs(raw_delta[0])))
                if angle <= axis_tolerance and any(raw_delta):
                    delta[1], attracted = 0, "horizontal"
                elif 90-angle <= axis_tolerance and any(raw_delta):
                    delta[0], attracted = 0, "vertical"
            finer = False
            if not any(delta) and any(abs(d) >= 0.5 for d in raw_delta):
                # Keep a distinguishable interval rather than silently erasing
                # it when this requested coarse grade has no usable sample.
                delta = [quantize(d, 1) for d in raw_delta]
                attracted, finer = None, True
            end_key = (s, span["endIndex"])
            if end_key not in acquired_sites:
                acquired_sites[end_key] = relative(f"sample-{s}-{span['endIndex']}", start, delta, raw_end)
            end = acquired_sites[end_key]
            controls = [start]
            for h, raw in enumerate(span["controls"][1:-1]):
                hd = [quantize(raw[j]-raw_start[j], quantum) for j in range(2)]
                controls.append(relative(f"handle-{span_id}-{h}", start, hd, raw))
            controls.append(end)
            edge = r.edge("fit-"+span_id, controls, f"Recorded stroke {s+1}, samples {span['startIndex']}–{span['endIndex']}; {span['kind']}")
            source_edges.append(edge)
            visible.append(edge)
            drawn.extend(control for control in controls if control not in drawn)
            mapping["spanEdges"][span_id] = [edge]
            mapping["edgeSpans"][edge] = [span_id]
            r.data["sourceMapping"]["spanMap"][span_id] = edge
            mapping["decisions"][span_id] = {"axisAttraction": attracted,
                "observedDisplacement": raw_delta, "requestedDisplacement": delta,
                "retainedFinerDisplacement": finer, "sourceStroke": s,
                "sourceRange": [span["startIndex"], span["endIndex"]]}
        retired.extend(outline.pop(s, []))
        remaining = [edge for edges in outline.values() for edge in edges]
        r.finish(visible+remaining, envelope+retired)
        mapping["sourceStrokes"].append({"strokeIndex": s, "spanIds": stroke["spans"],
                                          "edgeIds": source_edges, "rung": s+2})
    return r.data


def build(source_path, destination):
    source_path, destination = Path(source_path), Path(destination)
    destination.mkdir(parents=True, exist_ok=True)
    raw_bytes = source_path.read_bytes()
    source_lines = [line for line in raw_bytes.decode("utf-8-sig").splitlines() if line.strip()]
    records = [json.loads(line) for line in source_lines]
    observations, programs, packets = [], [], []
    for ordinal, source in enumerate(records, 1):
        observation = observe_record(source)
        observations.append(observation)
        for mode in MODES:
            program = construct(observation, ordinal, *mode)
            programs.append(program)
            packets.append({"source": source, "sourceOriginalJson": source_lines[ordinal-1],
                            "observation": observation, "program": program})
    for name, values in (("observations", observations), ("programs", programs), ("packets", packets)):
        (destination/(name+".ndjson")).write_text("".join(json.dumps(v, separators=(",", ":"), ensure_ascii=False)+"\n" for v in values), encoding="utf-8")
    (destination/"selected.ndjson").write_bytes(raw_bytes)
    manifest = {"schema": "gojo.quickdraw-interpretation-study.v1",
                "sourceSha256": hashlib.sha256(raw_bytes).hexdigest(),
                "sourceIds": [o["sourceId"] for o in observations],
                "modes": [{"id": m, "label": l, "quantum": q, "axisToleranceDegrees": a} for m, l, q, a in MODES],
                "counts": {"drawings": len(records), "programs": len(programs),
                           "strokes": sum(o["counts"]["strokes"] for o in observations),
                           "samples": sum(o["counts"]["samples"] for o in observations)},
                "trainingStatus": "No model trained; authored candidate thresholds and geometric interpretation policies.",
                "splitPolicy": "All interpretations, intervals and derivatives of one sourceId belong to the same future data split.",
                "selection": "The existing five visually curated church drawings, selected before this experiment; not a representative population evaluation."}
    (destination/"manifest.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    return manifest


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(build(args.input, args.output), indent=2))
