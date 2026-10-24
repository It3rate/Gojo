"""Independent acquisition, local-patch, affine replay and pass-accounting audit.

This module never imports the proposal generator or recipe author. Its finite
source-distance read is diagnostic; passing does not establish inferred intent,
optimality, a learned prior, or material transfer.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path

from verify_quickdraw_interpretation import (
    audit_observations, audit_geometric_candidates, audit_native_program,
    evaluate_program, curve_point, read_rows,
)


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def close(actual, expected, message):
    require(math.isfinite(actual) and math.isfinite(expected) and
            math.isclose(actual, expected, rel_tol=1e-8, abs_tol=1e-7), message)


def finite(value):
    if isinstance(value, float):
        require(math.isfinite(value), "Nonfinite proposal read")
    elif isinstance(value, dict):
        for child in value.values():
            finite(child)
    elif isinstance(value, list):
        for child in value:
            finite(child)


def point_segment(point, first, last):
    delta = [last[i]-first[i] for i in range(2)]
    square = sum(value*value for value in delta)
    parameter = max(0, min(1, sum((point[i]-first[i])*delta[i] for i in range(2))/square)) if square else 0
    return math.dist(point, [first[i]+parameter*delta[i] for i in range(2)])


def source_selection(observation, span_ids):
    spans = {span["id"]: span for span in observation["spans"]}
    point_keys, segment_keys = set(), set()
    for key in span_ids:
        require(key in spans, "Unknown acquired source interval")
        span = spans[key]
        point_keys.update((span["strokeIndex"], i) for i in range(span["startIndex"], span["endIndex"]+1))
        segment_keys.update((span["strokeIndex"], i, i+1) for i in range(span["startIndex"], span["endIndex"]))
        if span["startIndex"] == span["endIndex"]:
            segment_keys.add((span["strokeIndex"], span["startIndex"], span["startIndex"]))
    strokes = {stroke["index"]: stroke["points"] for stroke in observation["strokes"]}
    return ([strokes[s][i] for s, i in sorted(point_keys)],
            [(strokes[s][a], strokes[s][b]) for s, a, b in sorted(segment_keys)])


def measure(observation, span_ids, edges, subdivisions=24):
    """Every acquired event/segment participates once, irrespective of aliases."""
    points, source_segments = source_selection(observation, span_ids)
    proposal_points, proposal_segments = [], []
    for edge in edges:
        samples = [curve_point(edge["controls"], index/subdivisions) for index in range(subdivisions+1)]
        proposal_points.extend(samples)
        proposal_segments.extend(zip(samples, samples[1:]))
    if not points or not proposal_points:
        require(not points and not proposal_points, "Empty geometry cannot certify a fit to retained source")
        return dict.fromkeys(("rms", "max", "sourceRms", "proposalRms", "sourceMax", "proposalMax", "sourceTravel", "proposalTravel",
                             "sourceSampleCount", "sourceSegmentCount", "proposalSampleCount"), 0.0)
    forward = [min(point_segment(point, a, b) for a, b in proposal_segments) for point in points]
    backward = [min(point_segment(point, a, b) for a, b in source_segments) for point in proposal_points]
    source_square = sum(value*value for value in forward)/len(forward)
    proposal_square = sum(value*value for value in backward)/len(backward)
    return {"rms": math.sqrt((source_square+proposal_square)/2), "max": max(forward+backward),
            "sourceRms": math.sqrt(source_square), "proposalRms": math.sqrt(proposal_square),
            "sourceMax": max(forward), "proposalMax": max(backward),
            "sourceSampleCount": len(points), "sourceSegmentCount": len(source_segments), "proposalSampleCount": len(proposal_points),
            "sourceTravel": sum(math.dist(a, b) for a, b in source_segments),
            "proposalTravel": sum(math.dist(a, b) for a, b in proposal_segments)}


def edge_core(edge):
    return {key: edge[key] for key in ("id", "controls", "spans", "sourceRanges", "endpointKeys")}


def support(edges):
    return set(span for edge in edges for span in edge["spans"])


def audit_shared_sites(raw_edges, native_edges):
    """A declared common proposal site requires one actual native control row."""
    selected = {}
    for raw in raw_edges:
        keys = raw.get("siteKeys")
        if keys is None:
            keys = [raw["endpointKeys"][0]]+[None]*(len(raw["controls"])-2)+[raw["endpointKeys"][-1]]
        controls = native_edges["g-"+raw["id"]]["controls"]
        require(len(keys) == len(controls), "Raw/native control-site correspondence changed")
        for key, actual in zip(keys, controls):
            if key is None:
                continue
            require(key not in selected or selected[key] == actual,
                    f"Shared proposal site {key} split into distinct native control identities: {selected.get(key)} / {actual}")
            selected[key] = actual


def audit_affine_controls(raw, native_edges, ancestors):
    """Each selected affine output consumes its corresponding template control."""
    transform = raw.get("transform")
    if not transform:
        return
    source_ids = transform["sourceEdgeIds"]
    require(len(source_ids) == 1, "Affine profile requires one explicit native template")
    template = list(native_edges["g-"+source_ids[0]]["controls"])
    if transform.get("reversed"):
        template.reverse()
    output = native_edges["g-"+raw["id"]]["controls"]
    require(len(template) == len(output), "Affine template control correspondence changed")
    for source, target in zip(template, output):
        require(source in ancestors[target] | {target},
                "Affine output bypassed its actual corresponding template control")


def audit_proposal(observation, proposal):
    finite(proposal)
    require(proposal["sourceId"] == observation["sourceId"], "Proposal source identity changed")
    require(proposal.get("sourceDrawingSha256") == observation.get("sourceDrawingSha256"), "Proposal source hash changed")
    spans = {span["id"]: span for span in observation["spans"]}
    policy = proposal["policy"]
    require(policy["learned"] is False, "Authored coefficients cannot be presented as learned priors")
    require(policy["profileSubdivisions"] == 24, "Finite distance calibration changed")
    require(proposal["steps"] and len(proposal["steps"])-1 <= policy["maximumPasses"], "Undeclared pass budget")
    operation_ids, introduced, known = set(), set(), {}
    state, residue, accepted, rejected, results = {}, {}, 0, 0, []

    def check_edge(edge):
        require(edge["id"] and 2 <= len(edge["controls"]) <= 4 and all(len(p) == 2 for p in edge["controls"]), "Invalid finite profile")
        require(edge["spans"] and len(set(edge["spans"])) == len(edge["spans"]) and set(edge["spans"]) <= set(spans), "Unsupported/duplicate profile interval")
        require(len(edge["sourceRanges"]) == len(edge["spans"]), "Source ranges lost interval correspondence")
        for key, raw in zip(edge["spans"], edge["sourceRanges"]):
            require(raw["spanId"] == key and all(raw[field] == spans[key][field] for field in ("strokeIndex", "startIndex", "endIndex")), "Source interval endpoints changed")
        require(len(edge["endpointKeys"]) == 2, "Profile lost ordered endpoint references")

    def complexity(edges):
        return policy["edgeCost"]*len(edges)+policy["controlCost"]*sum(len(edge["controls"]) for edge in edges)

    def compare_metrics(actual, expected):
        for key, value in expected.items():
            close(actual[key], value, "Finite source/proposal metric changed: "+key)

    for index, step in enumerate(proposal["steps"]):
        final_main = {edge["id"]: edge for edge in step["edges"]}
        final_residue = {edge["id"]: edge for edge in step.get("residueEdges", [])}
        require(len(final_main) == len(step["edges"]) and len(final_residue) == len(step.get("residueEdges", [])), "Repeated active edge identity")
        require(not set(final_main) & set(final_residue), "Main and residue targets overlap")
        if index == 0:
            require(not step["operations"], "Baseline cannot hide prior simplification operations")
            require(len(final_main) == len(spans) and not final_residue, "Baseline must retain every fitted interval")
            require([edge["spans"] for edge in step["edges"]] == [[key] for key in spans], "Baseline changed acquired interval order")
            for edge in step["edges"]:
                require(edge["controls"] == spans[edge["spans"][0]]["controls"], "Baseline silently regularized the source fit")
            state, residue = dict(final_main), dict(final_residue)
            introduced.update(state)
        step_accepted = step_rejected = 0
        for operation in step["operations"]:
            require(operation["id"] not in operation_ids, "Repeated patch identity")
            operation_ids.add(operation["id"])
            before, after = operation["beforeEdges"], operation["afterEdges"]
            require([edge["id"] for edge in before] == operation["sourceEdgeIds"] and [edge["id"] for edge in after] == operation["targetEdgeIds"], "Patch participant order changed")
            require(before and after and len(set(operation["sourceEdgeIds"])) == len(before) and len(set(operation["targetEdgeIds"])) == len(after), "Invalid patch focus")
            for edge in before+after:
                check_edge(edge)
            require(all(edge["id"] in state and edge_core(edge) == edge_core(state[edge["id"]]) for edge in before), "Patch uses stale/future geometry")
            require(set(operation["sourceSpans"]) == support(before) == support(after), "Patch lost or invented acquired support")
            require(operation["reason"], "Candidate decision lost its qualification")
            moved_to_residue = operation["kind"] == "carry-detail-complement"
            if moved_to_residue:
                require(operation["accepted"] and [edge_core(e) for e in before] == [edge_core(e) for e in after], "Detail complement must retain geometry exactly")
                selected_points, selected_segments = source_selection(observation, support(before))
                minimum = [min(p[j] for p in selected_points) for j in range(2)]
                maximum = [max(p[j] for p in selected_points) for j in range(2)]
                close(operation["measurements"]["diameter"], math.dist(minimum, maximum), "Detail extent changed")
                close(operation["measurements"]["travel"], sum(math.dist(a, b) for a, b in selected_segments), "Retained detail travel changed")
            else:
                measurements = operation["measurements"]
                before_metric = measure(observation, support(before), before)
                after_metric = measure(observation, support(before), after)
                compare_metrics(measurements["before"], before_metric)
                compare_metrics(measurements["after"], after_metric)
                close(measurements["complexityBefore"], complexity(before), "Patch input complexity changed")
                close(measurements["complexityAfter"], complexity(after), "Patch output complexity changed")
                error_weight = operation["thresholds"].get("errorWeight", policy["errorWeight"])
                require(error_weight >= 0, "Negative measured-error weight has no declared local interpretation")
                for suffix, edges, metric in (("Before", before, before_metric), ("After", after, after_metric)):
                    score = error_weight*metric["rms"]**2+complexity(edges)+measurements["expectation"+suffix]
                    close(operation["score"+suffix], score, "Declared local objective changed")
                if operation["accepted"]:
                    require(after_metric["max"] <= operation["thresholds"]["maximumSymmetricDistance"]+1e-7, "Accepted patch exceeds source-distance allowance")
                    require(operation["scoreAfter"] < operation["scoreBefore"]-1e-7, "Accepted patch has no local objective improvement")
            if operation["kind"] == "center-observed-cross-arms":
                cross = next((candidate for candidate in observation.get("crosses", []) if candidate["id"] == operation["construction"]["existingCrossId"]), None)
                require(cross is not None and set(cross["spans"]) <= set(operation["sourceSpans"]), "Cross was invented without observed arms")
            if operation["accepted"]:
                for edge in before:
                    state.pop(edge["id"])
                for edge in after:
                    require(moved_to_residue or edge["id"] not in introduced, "Accepted replacement reused an existing identity")
                    (residue if moved_to_residue else state)[edge["id"]] = edge
                    introduced.add(edge["id"])
                accepted += 1; step_accepted += 1
            else:
                rejected += 1; step_rejected += 1
        require(set(state) == set(final_main) and set(residue) == set(final_residue), "Final pass differs from its accepted local patches")
        require(all(edge_core(edge) == edge_core(final_main[key]) for key, edge in state.items()) and
                all(edge_core(edge) == edge_core(final_residue[key]) for key, edge in residue.items()), "Pass geometry changed outside recorded patches")
        coordinates = {}
        for edge in list(final_main.values())+list(final_residue.values()):
            check_edge(edge)
            for key, point in zip(edge["endpointKeys"], (edge["controls"][0], edge["controls"][-1])):
                require(key not in coordinates or math.dist(coordinates[key], point) < 1e-7, "A shared proposal endpoint has competing coordinates")
                coordinates[key] = point
            if edge["id"] in known:
                require(known[edge["id"]] == edge_core(edge), "Retained geometry was silently overwritten")
            known[edge["id"]] = edge_core(edge)
        main_support, residue_support = support(final_main.values()), support(final_residue.values())
        require(not main_support & residue_support and main_support | residue_support == set(spans), "Active/complement source accounting is incomplete or overlapping")
        require(set(step["sourceCoverage"]["active"]) == main_support and set(step["sourceCoverage"]["complement"]) == residue_support, "Reported source coverage differs from actual profiles")
        require(step["residue"]["sourceSpanIds"] == list(spans), "Full original interval residue was dropped")
        compare_metrics(step["metrics"], measure(observation, main_support, list(final_main.values())))
        require(step["metrics"]["edgeCount"] == len(final_main) and step["metrics"]["residueEdgeCount"] == len(final_residue), "Main/residue counts changed")
        require(step["metrics"]["controlCount"] == sum(len(edge["controls"]) for edge in final_main.values()), "Control count changed")
        require(step["convergence"] == {"accepted": step_accepted, "rejected": step_rejected}, "Local decision count changed")
        results.append({"pass": index, "edges": len(final_main), "residueEdges": len(final_residue), "accepted": step_accepted, "rejected": step_rejected})
        state, residue = dict(final_main), dict(final_residue)
    require(proposal["convergence"]["globalOptimumClaimed"] is False and proposal["convergence"]["accepted"] == accepted, "Experimental convergence claim changed")
    return {"acceptedPatches": accepted, "rejectedPatches": rejected, "passes": results}


def audit_program(program, observation, proposal=None):
    mapping = program["sourceMapping"]
    require(mapping["sourceId"] == observation["sourceId"], "Construction source identity changed")
    spans = {span["id"] for span in observation["spans"]}
    values, dependencies = evaluate_program(program)
    edges = {edge["id"]: edge for edge in program["edges"]}
    sites = {site["id"]: site for site in program["sites"]}
    require(len(edges) == len(program["edges"]), "Repeated native edge identity")
    require(len(mapping["passes"]) == len(program["rungs"]), "Missing pass correspondence")
    ancestors = {}
    for key, refs in dependencies.items():
        ancestors[key] = set(refs)
        for ref in refs:
            ancestors[key].update(ancestors[ref])
        if sites[key]["at"] > 0:
            require(isinstance(sites[key]["x"], dict) and isinstance(sites[key]["y"], dict), "New pass injected coordinate literals")
            earlier = {site for rung in program["rungs"][:sites[key]["at"]] for edge_id in rung["visible"]+rung["guides"] for site in edges[edge_id]["controls"]}
            require(ancestors[key] & earlier, "New site lacks an actual earlier construction dependency")
    result, previous, displacement = [], set(), []
    for index, (row, rung) in enumerate(zip(mapping["passes"], program["rungs"])):
        require(row["rung"] == index, "Pass chronology differs from authored construction")
        visible, guides, residue = set(rung["visible"]), set(rung["guides"]), set(row["residueEdgeIds"])
        require(not visible & guides and residue <= guides, "Active residue must be explicitly selected guide geometry")
        require(set(rung["selected"]) == previous-visible, "Retired-edge accounting changed")
        require(previous-visible <= guides, "Replaced geometry was not retained as guidance")
        active = visible | residue; covered, targets = set(), set()
        require(active <= set(edges), "Absent target edge")
        require(all(edges[key]["at"] <= index and all(sites[site]["at"] <= edges[key]["at"] for site in edges[key]["controls"])
                    for key in visible | guides), "Rung consumes future geometry")
        require(set(rung["introduced"]) == {key for key, edge in edges.items() if edge["at"] == index}, "Introduced edge accounting changed")
        for link in row["links"]:
            selected, target = set(link["spanIds"]), set(link["edgeIds"])
            require(selected and target and selected <= spans and target <= active, "Unsupported or inactive source/target correspondence")
            require(len(selected) == len(link["spanIds"]) and len(target) == len(link["edgeIds"]), "Repeated participant in one link")
            covered.update(selected); targets.update(target)
        unmatched, inferred = set(row["unmatchedSpanIds"]), set(row["inferredEdgeIds"])
        require(len(unmatched) == len(row["unmatchedSpanIds"]) and len(inferred) == len(row["inferredEdgeIds"]) and len(residue) == len(row["residueEdgeIds"]), "Repeated pass-accounting participant")
        require(not covered & unmatched and covered | unmatched == spans, "Matched/unmatched source accounting changed")
        require(not targets & inferred and targets | inferred == active, "Active target correspondence is incomplete or ambiguous")
        if proposal is not None:
            step = proposal["steps"][index]
            require(visible == {"g-"+edge["id"] for edge in step["edges"]} and residue == {"g-"+edge["id"] for edge in step.get("residueEdges", [])}, "Native visibility differs from accepted proposal")
            audit_shared_sites(step["edges"]+step.get("residueEdges", []), edges)
            for raw in step["edges"]+step.get("residueEdges", []):
                native = edges["g-"+raw["id"]]
                require(len(native["controls"]) == len(raw["controls"]), "Native conversion changed curve degree")
                displacement.extend(math.dist(values[site], point) for site, point in zip(native["controls"], raw["controls"]))
                actual_support = set(span for link in row["links"] if native["id"] in link["edgeIds"] for span in link["spanIds"])
                require(actual_support == set(raw["spans"]), "Native source support differs from accepted proposal")
            for operation in step["operations"]:
                if not operation["accepted"] or operation["kind"] == "carry-detail-complement":
                    continue
                input_sites = {site for key in operation["sourceEdgeIds"] for site in edges["g-"+key]["controls"]}
                for key in operation["targetEdgeIds"]:
                    target = edges["g-"+key]
                    require(target["at"] == index, "Replacement edge has the wrong construction pass")
                    require(any((ancestors[site] | {site}) & input_sites for site in target["controls"]), "Replacement geometry has no actual selected-input dependency")
                for raw in operation["afterEdges"]:
                    audit_affine_controls(raw, edges, ancestors)
        result.append({"pass": index, "activeTargets": len(active), "matchedIntervals": len(covered), "unmatchedIntervals": len(unmatched)})
        previous = visible
    require(max(displacement, default=0) <= 2, "Native finite import exceeds the declared two-unit control allowance")
    return {"mapping": result, "maximumProposalControlDisplacement": max(displacement, default=0), "standaloneAffineReplay": True}


def verify(root, source=None):
    source = source or root/"selected.ndjson"
    records = {str(row["key_id"]): row for row in read_rows(source)}
    observations = {row["sourceId"]: row for row in read_rows(root/"observations.ndjson")}
    proposals = {row["sourceId"]: row for row in read_rows(root/"proposals.ndjson")}
    programs = read_rows(root/"programs.ndjson")
    runs = {run["id"]: run for run in read_rows(root/"native/inspector.ndjson")}
    require(set(records) == set(observations) == set(proposals) == {program["sourceMapping"]["sourceId"] for program in programs}, "Source membership changed")
    require(len(programs) == len(records) and {program["id"] for program in programs} == set(runs), "Native program membership changed")
    results = []
    for program in programs:
        key = program["sourceMapping"]["sourceId"]; observation, proposal = observations[key], proposals[key]
        preservation = audit_observations(records[key], observation)
        audit_geometric_candidates(records[key], observation)
        results.append({"sourceId": key, **preservation, **audit_proposal(observation, proposal),
                        **audit_program(program, observation, proposal), "native": audit_native_program(program, runs[program["id"]])})
    summary = {"schema": "gojo.quickdraw-gestalt-audit.v1", "passed": True, "drawings": len(results),
               "rungs": sum(len(program["rungs"]) for program in programs), "results": results,
               "sourceSha256": hashlib.sha256(Path(source).read_bytes()).hexdigest(),
               "programSha256": hashlib.sha256((root/"programs.ndjson").read_bytes()).hexdigest(),
               "scope": "Independent exact acquisition/interval preservation, local accepted/rejected patch replay, finite source fit and objective, actual affine dependencies, source/target pass accounting and native inspector replay. No intended-shape or learned-optimum claim."}
    (root/"verification.json").write_text(json.dumps(summary, indent=2)+"\n", encoding="utf-8")
    return summary


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--source", type=Path)
    arguments = parser.parse_args()
    result = verify(arguments.root, arguments.source)
    print(json.dumps({key: result[key] for key in ("passed", "drawings", "rungs")}))
