"""Independent audit of the source-linked, view-aware synthesis experiment.

This reader does not import the proposal generator or recipe author. It keeps
measured fit separate from an authored conceptual objective: late synthesis may
move away from both the acquired ink and its preceding interpretation.
"""
from __future__ import annotations

import argparse
from copy import deepcopy
from fractions import Fraction
import hashlib
import json
import math
from pathlib import Path

from verify_quickdraw_gestalt import (
    require, close, finite, edge_core, support, measure,
    audit_proposal as audit_gestalt_proposal,
    audit_program, audit_shared_sites,
)
from verify_quickdraw_interpretation import (
    audit_observations, audit_geometric_candidates, audit_native_program,
    evaluate_program, read_rows, curve_point,
)


def ancestry(program):
    values, dependencies = evaluate_program(program)
    ancestors = {}
    for key, refs in dependencies.items():
        ancestors[key] = set(refs)
        for ref in refs:
            ancestors[key].update(ancestors[ref])
    return values, ancestors


def control_keys(edge):
    keys = edge.get("siteKeys")
    if keys is None:
        keys = [edge["endpointKeys"][0]]+[None]*(len(edge["controls"])-2)+[edge["endpointKeys"][-1]]
    require(len(keys) == len(edge["controls"]), "Control/site-key correspondence changed")
    return keys


def coordinate_influence(program, target, axis, stops):
    """Affine influence at exposed input sites, before finite truncation.

    A zero/cancelled reference or an unrelated coordinate cannot manufacture a
    producer-consumer dependency. Each selected prior site is treated as an
    independent exposed input rather than expanded through its own history.
    """
    parameters = {row["id"]: row["value"] for row in program["parameters"]}
    forms = {}
    for site in program["sites"]:
        key = site["id"]
        if key in stops:
            forms[key] = {key: Fraction(1)}
        else:
            expression = site[("x", "y")[axis]]
            if type(expression) is int:
                forms[key] = {}
            else:
                numerator = expression["numerator"]
                numerator = parameters[numerator] if isinstance(numerator, str) else numerator
                ratio = Fraction(numerator, expression["denominator"])
                terms = {}
                for ref, coefficient in ((expression.get("origin", expression["from"]), Fraction(1)),
                                         (expression["to"], ratio), (expression["from"], -ratio)):
                    for selected, value in forms[ref].items():
                        terms[selected] = terms.get(selected, Fraction(0))+coefficient*value
                forms[key] = {selected: value for selected, value in terms.items() if value}
        if key == target:
            return forms[key]
    raise AssertionError("Missing target coordinate")


def audit_endpoint_bindings(operation, program, raw_inputs=None, tolerance=2):
    """A proposed shared joint consumes the actual selected earlier controls.

    Signed coordinate coefficients are affine instructions, not confidence
    values. Target identity is checked independently from coordinate agreement.
    """
    native = {edge["id"]: edge for edge in program["edges"]}
    values, ancestors = ancestry(program)
    raw = {edge["id"]: edge for edge in operation["beforeEdges"]}
    if raw_inputs:
        raw.update(raw_inputs)
    outputs = {}
    for edge in operation["afterEdges"]:
        actual = native["g-"+edge["id"]]
        require(len(actual["controls"]) == len(edge["controls"]), "Synthesis changed native curve degree")
        for key, point, site in zip(control_keys(edge), edge["controls"], actual["controls"]):
            if key is None:
                continue
            if key in outputs:
                require(outputs[key][1] == site, "Proposed joint has equal coordinates but distinct native identity")
                require(math.dist(outputs[key][0], point) < 1e-7, "One target joint has competing proposal coordinates")
            outputs[key] = point, site
    bound = set()
    bindings = operation.get("construction", {}).get("endpointBindings", [])
    for binding in bindings:
        key = binding["targetSiteKey"]
        require(key in outputs and key not in bound, "Binding target is absent or repeated")
        bound.add(key)
        point, site = outputs[key]
        for axis, name in enumerate(("x", "y")):
            inputs = binding.get(name+"Inputs", binding.get("inputs", []))
            if name == "x" and binding.get("mode") == "mirror" and "xInputs" not in binding:
                inputs = ([dict(item, weight=2*item["weight"]) for item in binding.get("axisInputs", [])]
                          +[dict(item, weight=-item["weight"]) for item in binding.get("inputs", [])])
            require(inputs, "Synthesis coordinate lacks declared prior controls")
            close(sum(item["weight"] for item in inputs), 1, "Synthesis coefficients are not an affine combination")
            expected, native_expected = 0.0, 0.0
            participants, coefficients = set(), {}
            for item in inputs:
                require(math.isfinite(item["weight"]), "Nonfinite synthesis coefficient")
                edge_id, index = item["edgeId"], item["controlIndex"]
                require(edge_id in raw and type(index) is int and 0 <= index < len(raw[edge_id]["controls"]),
                        "Binding consumes absent/future control geometry")
                source_site = native["g-"+edge_id]["controls"][index]
                expected += item["weight"]*raw[edge_id]["controls"][index][axis]
                native_expected += item["weight"]*values[source_site][axis]
                if item["weight"]:
                    participants.add(source_site)
                coefficients[source_site] = coefficients.get(source_site, Fraction(0))+Fraction(str(item["weight"]))
            close(point[axis], expected, "Proposed joint does not follow its declared affine input controls")
            require(participants <= ancestors[site] | {site}, f"Synthesized joint {operation.get('id')}/{key}/{name} bypasses a declared actual prior control: {participants-(ancestors[site] | {site})}")
            exposed = {control for edge in operation["beforeEdges"] for control in native["g-"+edge["id"]]["controls"]} | participants
            actual_coefficients = coordinate_influence(program, site, axis, exposed)
            expected_coefficients = {selected: value for selected, value in coefficients.items() if value}
            require(set(actual_coefficients) == set(expected_coefficients) and
                    all(math.isclose(float(actual_coefficients[selected]), float(value), abs_tol=1e-8) for selected, value in expected_coefficients.items()),
                    "Synthesized coordinate bypasses its actual nonzero affine input influences")
            require(abs(values[site][axis]-native_expected) <= tolerance,
                    "Native joint differs from its declared finite affine input read")
    return {"boundTargetSites": len(bound), "nativeSharedSiteChecks": len(outputs)}


def audit_preserved_prefix(observation, proposal, original=None):
    require(len(proposal["steps"]) >= 4, "Synthesis omitted an original interpretation stage")
    prefix = deepcopy(proposal)
    prefix["steps"] = prefix["steps"][:4]
    prefix["convergence"] = {"globalOptimumClaimed": False,
        "accepted": sum(operation["accepted"] for step in prefix["steps"] for operation in step["operations"])}
    result = audit_gestalt_proposal(observation, prefix)
    if original is not None:
        require(prefix["steps"] == original["steps"], "Late synthesis rewrote the preserved original interpretation")
    return result


def synthesis_core(edge):
    return edge_core(edge) | {"siteKeys": edge.get("siteKeys")}


def compare_metric(actual, expected):
    for key, value in expected.items():
        close(actual[key], value, "Synthesis finite metric changed: "+key)


def samples(edge):
    return [curve_point(edge["controls"], index/24) for index in range(25)]


def profile_delta(before, after):
    old = {edge["id"]: edge for edge in before}
    distances = []
    for edge in after:
        parent_ids = edge.get("previousEdgeIds", edge.get("parents", []))
        parents = [old[key] for key in parent_ids if key in old]
        require(parents, "Self-displacement read lost its actual immediate input profiles")
        points = [point for parent in parents for point in samples(parent)]
        distances.extend(min(math.dist(point, source) for source in points) for point in samples(edge))
    return {"rms": math.sqrt(sum(value*value for value in distances)/len(distances)) if distances else 0,
            "max": max(distances, default=0), "sampleCount": len(distances)}


def constraint_read(operation, raw_inputs):
    """Calculate only the finite structural read explicitly selected by a patch."""
    kind, construction = operation["kind"], operation["construction"]
    before, after = operation["beforeEdges"], operation["afterEdges"]
    source = dict(raw_inputs) | {edge["id"]: edge for edge in before}
    def point(ref):
        require(ref["edgeId"] in source and 0 <= ref["controlIndex"] < len(source[ref["edgeId"]]["controls"]), "Constraint read consumes absent geometry")
        return source[ref["edgeId"]]["controls"][ref["controlIndex"]]
    def coordinate(refs, axis):
        close(sum(ref["weight"] for ref in refs), 1, "Constraint basis is not an affine read")
        return sum(ref["weight"]*point(ref)[axis] for ref in refs)
    if kind == "merge-nearby-proposal-joints":
        total = 0
        for cluster in construction["jointClusters"]:
            points = [point(row) for row in cluster["inputs"]]
            center = [coordinate(cluster["inputs"], axis) for axis in range(2)]
            close(math.dist(center, cluster["point"]), 0, "Joint cluster center changed")
            spread = sum(math.dist(p, center)**2 for p in points)/len(points)
            close(cluster["spreadBefore"], spread, "Joint spread read changed")
            close(cluster["spreadAfter"], 0, "Merged target joint must have zero internal spread")
            total += spread
        return total, 0
    if kind == "complete-supported-upper-arch":
        return (before[0]["controls"][0][1]-before[1]["controls"][-1][1])**2, 0
    if kind == "absorb-short-outline-return":
        points = samples(before[-1])
        return sum(math.dist(a, b) for a, b in zip(points, points[1:]))**2, 0
    if kind == "synthesize-local-rectangular-facade" and construction.get("engine") == "rectangle":
        return 0, 0
    if kind in ("synthesize-local-rectangular-facade", "synthesize-reflected-upright-outline"):
        ids = construction.get("constraintEdgeIds", [edge["id"] for edge in before])
        selected = [source[key] for key in ids]
        axis = coordinate(construction["axisInputs"], 0)
        points = [point for edge in selected for point in samples(edge)]
        return sum(min((p[0]+q[0]-2*axis)**2+(p[1]-q[1])**2 for q in points) for p in points)/max(1, len(points)), 0
    if kind in ("align-supported-cross-to-upper-part", "align-supported-lower-door"):
        points = [p for edge in before for p in edge["controls"]]
        center = (min(p[0] for p in points)+max(p[0] for p in points))/2
        return (center-coordinate(construction["axisInputs"], 0))**2, 0
    if kind in ("attach-centered-roof-to-facade-corners", "synthesize-supported-side-parallelogram"):
        return 0, 0
    raise AssertionError("Undeclared conceptual constraint read: "+kind)


def audit_objective(operation, before_metric, after_metric, raw_inputs):
    measurements, thresholds = operation["measurements"], operation["thresholds"]
    require(operation.get("selectionPolicy") and "ordered" in operation["selectionPolicy"], "Late acceptance lacks an explicit ordered synthesis basis")
    require(thresholds.get("rawDistanceIsAcceptanceGate") is False, "Late source-distance diagnostic was presented as a hidden acceptance gate")
    compare_metric(measurements["originalBefore"], before_metric)
    compare_metric(measurements["originalAfter"], after_metric)
    delta = profile_delta(operation["beforeEdges"], operation["afterEdges"])
    compare_metric(measurements["previousPass"], delta)
    constraint_before, constraint_after = constraint_read(operation, raw_inputs)
    close(measurements["constraintBefore"], constraint_before, "Declared conceptual input residual changed")
    close(measurements["constraintAfter"], constraint_after, "Declared conceptual output residual changed")
    weights = thresholds["policyWeights"]
    require(set(weights) == {"originalInk", "previousInterpretation", "geometricPrior"} and all(math.isfinite(v) and v >= 0 for v in weights.values()), "Invalid or hidden synthesis objective calibration")
    close(operation["scoreBefore"], weights["originalInk"]*before_metric["rms"]**2+weights["geometricPrior"]*constraint_before, "Declared synthesis input score changed")
    close(operation["scoreAfter"], weights["originalInk"]*after_metric["rms"]**2+weights["previousInterpretation"]*delta["rms"]**2+weights["geometricPrior"]*constraint_after, "Declared synthesis output score changed")


def audit_native_correspondence(program, proposal):
    """Check active controls without relaxing the older two-tick import promise."""
    values, _ = ancestry(program)
    native = {edge["id"]: edge for edge in program["edges"]}
    declared = {row["operationId"]: row for row in program.get("synthesisMapping", [])}
    require(len(declared) == len(program.get("synthesisMapping", [])), "Repeated native synthesis operation correspondence")
    visited = set()
    late_tolerance = proposal.get("completionPolicy", proposal["policy"]).get("nativeControlTolerance", 6)
    require(0 < late_tolerance <= 6, "Undeclared or widened synthesis import resolution")
    displacement, maximum = [], 0
    for index, (step, row, rung) in enumerate(zip(proposal["steps"], program["sourceMapping"]["passes"], program["rungs"])):
        active = step["edges"]+step.get("residueEdges", [])
        require(set(rung["visible"]) == {"g-"+edge["id"] for edge in step["edges"]}, "Synthesis/native main profiles differ")
        require(set(row["residueEdgeIds"]) == {"g-"+edge["id"] for edge in step.get("residueEdges", [])}, "Synthesis/native active complement differs")
        audit_shared_sites(active, native)
        for edge in active:
            actual = native["g-"+edge["id"]]
            require(len(actual["controls"]) == len(edge["controls"]), "Synthesis native degree changed")
            distances = [math.dist(values[site], point) for site, point in zip(actual["controls"], edge["controls"])]
            tolerance = 2 if index < 4 else late_tolerance
            require(max(distances, default=0) <= tolerance, "Native control exceeds its declared synthesis import allowance")
            displacement.extend(distances)
            actual_support = set(span for link in row["links"] if actual["id"] in link["edgeIds"] for span in link["spanIds"])
            require(actual_support == set(edge["spans"]), "Synthesis native acquired support differs")
        for operation in step["operations"]:
            construction = operation.get("construction", {})
            bindings = construction.get("endpointBindings", [])
            if not operation["accepted"] or not bindings:
                continue
            key = f"r{index}-{operation['id']}"
            require(key in declared, "Native synthesis correspondence omitted an executed binding")
            visited.add(key)
            row, controls = declared[key], {}
            require(row["rung"] == index, "Native synthesis correspondence moved to another pass")
            for edge in operation["afterEdges"]:
                for token, site in zip(control_keys(edge), native["g-"+edge["id"]]["controls"]):
                    if token is not None:
                        require(token not in controls or controls[token] == site, "Synthesis map split a shared target identity")
                        controls[token] = site
            expected = {binding["targetSiteKey"]: controls[binding["targetSiteKey"]] for binding in bindings}
            require({binding["targetSiteKey"]: binding["siteId"] for binding in row["bindings"]} == expected, "Native synthesis binding map does not select its actual output controls")
            expected_pairs = {(controls[pair["firstSiteKey"]], controls[pair["secondSiteKey"]]) for pair in construction.get("symmetryPairs", [])}
            require({(pair["firstSiteId"], pair["secondSiteId"]) for pair in row["symmetryPairs"]} == expected_pairs, "Native reflection correspondence differs from the declared paired outputs")
            for plural in ("axisRectangles", "parallelograms"):
                frames = [{name.replace("Key", "Id"): controls[token] for name, token in frame.items()} for frame in construction.get(plural, [])]
                require(row.get(plural, []) == frames, "Native frame correspondence differs from its actual selected corner controls")
        maximum = max(maximum, max(displacement, default=0))
    require(visited == set(declared), "Native synthesis correspondence contains an unexecuted/stale binding")
    return {"maximumProposalControlDisplacement": maximum, "lateNativeControlTolerance": late_tolerance}


def audit_native_symmetry(program):
    """Mirror relations use one actual axis site and the actual first endpoint."""
    values, _ = ancestry(program)
    actual_controls = {site for edge in program["edges"] for site in edge["controls"]}
    checks, rectangles, parallelograms, pairs = 0, 0, 0, set()
    for operation in program.get("synthesisMapping", []):
        require(len({row["targetSiteKey"] for row in operation["bindings"]}) == len(operation["bindings"]), "Repeated native synthesis binding")
        for binding in operation["bindings"]:
            require(binding["siteId"] in actual_controls, "Declared synthesis site is not an actual target control")
        for pair in operation["symmetryPairs"]:
            first, second, axis = (pair[key] for key in ("firstSiteId", "secondSiteId", "axisSiteId"))
            require(first in actual_controls and second in actual_controls and axis in values, "Symmetry did not bind actual target controls and axis")
            require(values[first][0]+values[second][0] == 2*values[axis][0] and values[first][1] == values[second][1], "Native paired endpoints are not exactly mirrored")
            require(coordinate_influence(program, second, 0, {first, axis}) == {first: Fraction(-1), axis: Fraction(2)} and
                    coordinate_influence(program, second, 1, {first, axis}) == {first: Fraction(1)},
                    "Equal mirrored coordinates do not establish actual reflection dependency")
            pair_key = first, second, axis
            checks += pair_key not in pairs
            pairs.add(pair_key)
        for rectangle in operation.get("axisRectangles", []):
            tl, tr, bl, br = (rectangle[key] for key in ("topLeftSiteId", "topRightSiteId", "bottomLeftSiteId", "bottomRightSiteId"))
            require({tl, tr, bl, br} <= actual_controls, "Rectangle corners do not select actual target controls")
            require(values[tl][0] == values[bl][0] and values[tr][0] == values[br][0] and values[tl][1] == values[tr][1] and values[bl][1] == values[br][1] and
                    values[tl][0] < values[tr][0] and values[tl][1] < values[bl][1], "Native axis rectangle is not an upright nondegenerate box")
            rectangles += 1
        for frame in operation.get("parallelograms", []):
            a, b, c, d = (frame[key] for key in ("aSiteId", "bSiteId", "cSiteId", "dSiteId"))
            require({a, b, c, d} <= actual_controls, "Parallelogram corners do not select actual target controls")
            require(all(values[d][axis] == values[b][axis]+values[c][axis]-values[a][axis] for axis in range(2)), "Native side plane does not close its parallelogram")
            for axis in range(2):
                require(coordinate_influence(program, d, axis, {a, b, c}) == {a: Fraction(-1), b: Fraction(1), c: Fraction(1)}, "Parallelogram closure is numeric equality without actual corner dependencies")
            parallelograms += 1
    return {"nativeExactReflectionPairs": checks, "nativeAxisRectangles": rectangles, "nativeSideParallelograms": parallelograms}


def audit_view(step, known, program):
    """Audit the support of a revisable view read without claiming recognition."""
    view = step["viewHypothesis"]
    require(view["kind"] in ("front", "oblique", "unresolved") and view.get("reason"), "Missing or undeclared view basis")
    native = {edge["id"]: edge for edge in program["edges"]}
    def evidence(ids):
        require(len(ids) == len(set(ids)) and all(key in known and "g-"+key in native for key in ids), "View read lacks actual retained geometry support")
    evidence(view["supportEdgeIds"])
    candidates = view.get("candidates", [])
    if view["kind"] != "unresolved":
        require({row["kind"] for row in candidates} == {"front", "oblique"} and len(candidates) == 2, "View selection discarded its alternate reading")
        evidence(view["frontEdgeIds"])
        require(view["frontEdgeIds"], "View selection lacks a local facade basis")
    for candidate in candidates:
        require(0 <= candidate["support"] <= 1 and candidate.get("reason"), "Uncalibrated or unsupported view candidate")
        evidence(candidate["supportEdgeIds"])
        require(candidate["supportEdgeIds"] or candidate["support"] == 0, "An empty view candidate cannot claim positive geometric support")
    if view["kind"] == "oblique":
        side = view.get("side")
        require(side and side["depthSign"] in (-1, 1) and side["depth"] > 0 and side["supportEdgeIds"], "Oblique view lost its distinct supported side plane")
        evidence(side["supportEdgeIds"])
        require(set(side["supportEdgeIds"])-set(view["frontEdgeIds"]), "Oblique side plane merely aliases the facade")
        for operation in step["operations"]:
            if operation["kind"] in ("synthesize-local-rectangular-facade", "synthesize-reflected-upright-outline"):
                construction = operation["construction"]
                local = construction.get("viewFrame") == "localFront" or (
                    construction.get("partFrames") and all(frame.get("viewFrame") == "localFront" for frame in construction["partFrames"]))
                require(local, "Oblique synthesis applied global frontal reflection")
    return {"kind": view["kind"], "retainedAlternatives": len(candidates), "supportProfiles": len(view["supportEdgeIds"])}


def audit_late_proposal(observation, proposal, program):
    finite(proposal)
    require(proposal["policy"]["learned"] is False, "Authored synthesis cannot be presented as learned")
    require(len(proposal["steps"]) == 7, "This experiment declares three late synthesis stages")
    policy = proposal["completionPolicy"]
    require(policy["learned"] is False and len(policy["schedule"]) == 3, "Late authored policy/schedule is missing")
    for first, second in zip(policy["schedule"], policy["schedule"][1:]):
        require(first["originalInk"] > second["originalInk"] >= 0 and first["previousInterpretation"] > second["previousInterpretation"] >= 0 and
                first["geometricPrior"] < second["geometricPrior"], "Declared late influence decay changed")
    spans = {span["id"]: span for span in observation["spans"]}
    old = proposal["steps"][3]
    state = {edge["id"]: edge for edge in old["edges"]}
    residue = {edge["id"]: edge for edge in old.get("residueEdges", [])}
    known = {edge["id"]: edge for step in proposal["steps"][:4] for edge in step["edges"]+step.get("residueEdges", [])}
    for step in proposal["steps"][:4]:
        for operation in step["operations"]:
            if operation["accepted"]:
                known.update({edge["id"]: edge for edge in operation["afterEdges"]})
    ids = {operation["id"] for step in proposal["steps"][:4] for operation in step["operations"]}
    accepted = rejected = binding_count = 0
    results = []

    def check_edge(edge):
        require(edge["id"] and 2 <= len(edge["controls"]) <= 4 and all(len(p) == 2 for p in edge["controls"]), "Invalid synthesis profile")
        require(len(edge["spans"]) == len(set(edge["spans"])) and set(edge["spans"]) <= set(spans), "Unknown or repeated synthesis source interval")
        require(edge["spans"] or edge.get("inferred"), "Source-free synthesized ink must be explicitly inferred")
        require(len(edge["sourceRanges"]) == len(edge["spans"]), "Synthesis lost interval endpoint correspondence")
        for key, raw in zip(edge["spans"], edge["sourceRanges"]):
            require(raw["spanId"] == key and all(raw[field] == spans[key][field] for field in ("strokeIndex", "startIndex", "endIndex")), "Synthesis changed an acquired interval")
        require(len(edge["endpointKeys"]) == 2, "Synthesized profile lost endpoint topology")
        control_keys(edge)

    for index, step in enumerate(proposal["steps"][4:], 4):
        step_accepted = step_rejected = 0
        for operation in step["operations"]:
            require(operation["id"] not in ids, "Synthesis reused a patch identity")
            ids.add(operation["id"])
            before, after = operation["beforeEdges"], operation["afterEdges"]
            require(before and after and [edge["id"] for edge in before] == operation["sourceEdgeIds"] and [edge["id"] for edge in after] == operation["targetEdgeIds"], "Synthesis patch participant order changed")
            require(len({edge["id"] for edge in before}) == len(before) and len({edge["id"] for edge in after}) == len(after), "Repeated synthesis patch participant")
            for edge in before+after:
                check_edge(edge)
            require(all(edge["id"] in state and synthesis_core(edge) == synthesis_core(state[edge["id"]]) for edge in before), "Synthesis patch consumes stale/future profiles")
            require(set(operation["sourceSpans"]) == support(before) == support(after), "Synthesis patch lost or invented source support")
            require(operation.get("reason"), "Synthesis decision lost its interpretation qualification")
            carried = operation["kind"] in ("carry-collapsed-joint-detail", "carry-redundant-cross-detail")
            if carried:
                require(operation["accepted"] and [synthesis_core(edge) for edge in before] == [synthesis_core(edge) for edge in after] and
                        operation["measurements"].get("retainedExactly") is True, "Collapsed detail must remain exactly represented")
                for edge in before:
                    state.pop(edge["id"])
                    residue[edge["id"]] = edge
                accepted += 1; step_accepted += 1
                continue
            measurements = operation["measurements"]
            before_metric, after_metric = measure(observation, support(before), before), measure(observation, support(before), after)
            compare_metric(measurements["before"], before_metric)
            compare_metric(measurements["after"], after_metric)
            require(operation["thresholds"]["policyWeights"] == step["policyWeights"] == policy["schedule"][index-4], "Patch and pass use different declared influence schedules")
            audit_objective(operation, before_metric, after_metric, known)
            for role in ("cross-region", "door-region"):
                require(support(edge for edge in after if edge.get("role") == role) <= support(edge for edge in before if edge.get("role") == role),
                        "Synthesis invented an unsupported church part")
            if operation["kind"] == "align-supported-cross-to-upper-part":
                require(any(set(candidate["spans"]) <= support(before) for candidate in observation.get("crosses", [])),
                        "Synthesis invented cross arms without observed crossing support")
            if operation["accepted"]:
                require(all(edge["id"] not in known for edge in after), "Synthesis overwrote a retained target identity")
                checks = audit_endpoint_bindings(operation, program, known, policy.get("nativeControlTolerance", 6))
                binding_count += checks["boundTargetSites"]
                for edge in before:
                    state.pop(edge["id"])
                for edge in after:
                    state[edge["id"]] = edge
                    known[edge["id"]] = edge
                accepted += 1; step_accepted += 1
            else:
                rejected += 1; step_rejected += 1
        final, complement = {edge["id"]: edge for edge in step["edges"]}, {edge["id"]: edge for edge in step.get("residueEdges", [])}
        require(len(final) == len(step["edges"]) and len(complement) == len(step.get("residueEdges", [])), "Repeated late active identity")
        require(set(state) == set(final) and set(residue) == set(complement), "Late pass differs from its accepted patches")
        require(all(synthesis_core(edge) == synthesis_core(final[key]) for key, edge in state.items()) and
                all(synthesis_core(edge) == synthesis_core(complement[key]) for key, edge in residue.items()), "Synthesis silently changed retained geometry")
        for edge in list(final.values())+list(complement.values()):
            check_edge(edge)
        main_support, remainder_support = support(final.values()), support(complement.values())
        require(not main_support & remainder_support and main_support | remainder_support == set(spans), "Late source/complement accounting changed")
        require(set(step["sourceCoverage"]["active"]) == main_support and set(step["sourceCoverage"]["complement"]) == remainder_support, "Late reported source support changed")
        require(step["residue"]["sourceSpanIds"] == list(spans), "Synthesis discarded retained original ink")
        compare_metric(step["metrics"], measure(observation, main_support, list(final.values())))
        require(step["metrics"]["edgeCount"] == len(final) and step["metrics"]["residueEdgeCount"] == len(complement), "Late profile counts changed")
        require(step["metrics"]["controlCount"] == sum(len(edge["controls"]) for edge in final.values()), "Late control count changed")
        require(step["convergence"] == {"accepted": step_accepted, "rejected": step_rejected}, "Late decision count changed")
        results.append({"pass": index, "edges": len(final), "residueEdges": len(complement), "accepted": step_accepted, "rejected": step_rejected,
                        "view": audit_view(step, known, program)})
    require(proposal["convergence"]["globalOptimumClaimed"] is False, "Ordered synthesis cannot claim a global optimum")
    require(proposal["convergence"]["passes"] == len(proposal["steps"])-1 and proposal["convergence"]["accepted"] ==
            sum(operation["accepted"] for step in proposal["steps"] for operation in step["operations"]), "Synthesis history count changed")
    return {"lateAcceptedPatches": accepted, "lateRejectedPatches": rejected, "lateBoundSites": binding_count,
            "synthesisPasses": results, **audit_native_correspondence(program, proposal), **audit_native_symmetry(program)}


def verify(root, source=None, original_root=None):
    """Audit a completed portable run without executing its proposal adapter."""
    source = source or root/"selected.ndjson"
    records = {str(row["key_id"]): row for row in read_rows(source)}
    exact_lines = {str(json.loads(line)["key_id"]): line for line in Path(source).read_text(encoding="utf-8-sig").splitlines() if line.strip()}
    observations = {row["sourceId"]: row for row in read_rows(root/"observations.ndjson")}
    proposals = {row["sourceId"]: row for row in read_rows(root/"proposals.ndjson")}
    originals = ({row["sourceId"]: row for row in read_rows(original_root/"proposals.ndjson")} if original_root else {})
    programs = read_rows(root/"programs.ndjson")
    packets = {packet["observation"]["sourceId"]: packet for packet in read_rows(root/"packets.ndjson")}
    runs = {run["id"]: run for run in read_rows(root/"native/inspector.ndjson")}
    native_evidence = {row["id"]: row for row in json.loads((root/"native-evidence/summary.json").read_text(encoding="utf-8-sig"))}
    require(set(records) == set(observations) == set(proposals) == {program["sourceMapping"]["sourceId"] for program in programs}, "Source membership changed")
    require(len(programs) == len(records) and {program["id"] for program in programs} == set(runs) == set(native_evidence), "Native program membership changed")
    require(set(packets) == set(records), "Exact source packet membership changed")
    results = []
    for program in programs:
        key = program["sourceMapping"]["sourceId"]
        observation, proposal = observations[key], proposals[key]
        packet, evidence = packets[key], native_evidence[program["id"]]
        require(packet["sourceOriginalJson"] == exact_lines[key] and packet["source"] == records[key], "Exact original acquisition payload changed")
        require(packet["observation"] == observation and packet["proposal"] == proposal and packet["program"] == program, "Native packet disagrees with the audited source/target construction")
        require(all(evidence[field] is True for field in ("nativeRestoreVerified", "actualSourceAndTargetAliasesVerified", "completeSourcePartitionVerified",
                    "standaloneConstructionReplayVerified", "priorControlDependenciesVerified", "acquiredIdentityUnwelded")), "Native source/target identity transport failed")
        require(evidence["materialTransfer"] is False and evidence["exactGeometricFitClaimed"] is False, "Synthesis source read cannot claim exact fit or material transfer")
        preservation = audit_observations(records[key], observation)
        audit_geometric_candidates(records[key], observation)
        prefix = audit_preserved_prefix(observation, proposal, originals.get(key))
        late = audit_late_proposal(observation, proposal, program)
        transport = audit_program(program, observation)
        results.append({"sourceId": key, **preservation, "preservedPrefix": prefix, **transport, **late,
                        "native": audit_native_program(program, runs[program["id"]])})
    summary = {"schema": "gojo.quickdraw-synthesis-audit.v1", "passed": True, "drawings": len(results),
        "rungs": sum(len(program["rungs"]) for program in programs), "results": results,
        "sourceSha256": hashlib.sha256(Path(source).read_bytes()).hexdigest(),
        "programSha256": hashlib.sha256((root/"programs.ndjson").read_bytes()).hexdigest(),
        "scope": "Independent exact acquisition and interval preservation, original interpretation preservation, declared late affine joint identities and dependencies, conceptual-basis diagnostics, source/target accounting and standalone native replay. Authored exploratory priors; no learned, optimal or recovered-intent claim."}
    (root/"verification.json").write_text(json.dumps(summary, indent=2)+"\n", encoding="utf-8")
    return summary


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--source", type=Path)
    parser.add_argument("--original-root", type=Path)
    arguments = parser.parse_args()
    result = verify(arguments.root, arguments.source, arguments.original_root)
    print(json.dumps({key: result[key] for key in ("passed", "drawings", "rungs")}))
