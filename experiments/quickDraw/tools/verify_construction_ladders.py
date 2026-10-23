"""Independent numeric/source audit and causal transition export for the pilot."""
from __future__ import annotations

import argparse
import hashlib
import json
from fractions import Fraction
from pathlib import Path


def read_lines(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def parameters_used(sites):
    return {expression["numerator"] for site in sites for expression in (site["x"], site["y"])
            if isinstance(expression, dict) and isinstance(expression["numerator"], str)}


def evaluate(program):
    parameters = {p["id"]: p["value"] for p in program["parameters"]}
    values, ordinals, dependencies, rounding = {}, {}, {}, []
    for ordinal, site in enumerate(program["sites"]):
        result, refs = [], set()
        for axis, key in enumerate(("x", "y")):
            expression = site[key]
            if isinstance(expression, int):
                result.append(expression)
                continue
            first, second = expression["from"], expression["to"]
            origin = expression.get("origin", first)
            for source in (origin, first, second):
                assert source in values, (site["id"], "missing or forward reference", source)
                assert ordinals[source][1] <= site["at"], (site["id"], "future rung", source)
                refs.add(source)
            numerator = expression["numerator"]
            numerator = parameters[numerator] if isinstance(numerator, str) else numerator
            displacement = Fraction((values[second][axis] - values[first][axis]) * numerator,
                                    expression["denominator"])
            truncated = int(displacement)
            result.append(values[origin][axis] + truncated)
            if displacement.denominator != 1:
                remainder = displacement - truncated
                rounding.append({"site": site["id"], "axis": key,
                                 "coordinateRemainder": [remainder.numerator, remainder.denominator]})
        assert site["id"] not in values
        values[site["id"]] = result
        ordinals[site["id"]] = (ordinal, site["at"])
        dependencies[site["id"]] = refs
    return values, dependencies, rounding


def transition(program, run, index):
    """Context has only past sites; future coordinates are confined to target."""
    before, after = program["rungs"][index:index + 2]
    site_context = [s for s in program["sites"] if s["at"] <= index]
    site_instruction = [s for s in program["sites"] if s["at"] == index + 1]
    edge_context = [e for e in program["edges"] if e["at"] <= index]
    edge_instruction = [e for e in program["edges"] if e["at"] == index + 1]
    parameters = {p["id"]: p["value"] for p in program["parameters"]}
    past_parameters = parameters_used(site_context)
    new_parameters = parameters_used(site_instruction) - past_parameters
    restored_before, restored_after = run["rungs"][index:index + 2]
    statuses = {condition["status"] for condition in restored_after["constraints"]}
    outcome = ("failed-authored-condition" if "failed" in statuses else
               "open-authored-condition" if "open" in statuses else
               "satisfies-selected-conditions" if statuses else "no-conditions-selected")
    context_ids = {s["id"] for s in site_context}
    assert all(s["id"] in context_ids for s in restored_before["sites"])
    available = set(context_ids)
    for site in site_instruction:
        for expression in (site["x"], site["y"]):
            if isinstance(expression, dict):
                assert {expression["from"], expression["to"], expression.get("origin", expression["from"])} <= available
        available.add(site["id"])
    return {"schema": "gojo.authored-construction-transition.v1",
            "id": f"{program['id']}:{before['id']}->{after['id']}",
            "family": program["family"], "run": program["id"],
            "input": {"rung": before["id"], "viewBox": program["viewBox"],
                      "parameters": {key: parameters[key] for key in sorted(past_parameters)},
                      "siteInstructions": site_context, "edgeInstructions": edge_context,
                      "sites": restored_before["sites"], "edges": restored_before["edges"],
                      "visible": before["visible"], "guides": before["guides"],
                      "conditionRequests": [c for c in program["constraints"] if c["at"] <= index],
                      "conditionReads": restored_before["constraints"]},
            "instruction": {"parameters": {key: parameters[key] for key in sorted(new_parameters)},
                            "sites": site_instruction, "edges": edge_instruction,
                            "selected": after["selected"], "introduced": after["introduced"],
                            "visible": after["visible"], "guides": after["guides"],
                            "repeats": [r for r in program["repeats"] if r["at"] == index + 1],
                            "circular": [c for c in program["circular"] if c["at"] == index + 1],
                            "constraints": [c for c in program["constraints"] if c["at"] == index + 1]},
            "target": {"rung": after["id"], "sites": restored_after["sites"], "edges": restored_after["edges"],
                       "visible": after["visible"], "guides": after["guides"], "constraints": restored_after["constraints"],
                       "outcome": outcome},
            "provenance": {"authored": True, "nativeReplayVerified": run["nativeReplayVerified"],
                           "splitUnit": program["family"], "trainingRun": False}}


def verify(root):
    programs = read_lines(root / "programs.ndjson")
    runs = {row["id"]: row for row in read_lines(root / "native/inspector.ndjson")}
    checks, transitions, summaries = 0, [], []
    for program in programs:
        run = runs[program["id"]]
        assert run["nativeReplayVerified"] and run["nativeRestoreVerified"]
        expected, deps, rounding = evaluate(program)
        edge_source = {e["id"]: e for e in program["edges"]}
        for index, rung in enumerate(program["rungs"]):
            actual = run["rungs"][index]
            expected_sites = {s["id"] for s in program["sites"] if s["at"] <= index}
            assert {s["id"] for s in actual["sites"]} == expected_sites
            for site in actual["sites"]:
                assert [site["x"], site["y"]] == expected[site["id"]], (program["id"], site["id"])
                assert set(site["dependencies"]) == deps[site["id"]]
                checks += 2
            assert {e["id"] for e in actual["edges"] if not e["guide"]} == set(rung["visible"])
            assert {e["id"] for e in actual["edges"] if e["guide"]} == set(rung["guides"])
            for edge in actual["edges"]:
                source = edge_source[edge["id"]]
                assert edge["controlSiteIds"] == source["controls"]
                assert edge["controls"] == [expected[s] for s in source["controls"]]
                checks += 2
            prior = program["rungs"][index - 1]["visible"] if index else []
            assert set(rung["selected"]) == set(prior) - set(rung["visible"])
            assert not set(rung["visible"]) & set(rung["guides"])
            checks += 4
            if index + 1 < len(program["rungs"]):
                transitions.append(transition(program, run, index))
        if program["family"] == "church-front":
            repeat = program["repeats"][0]
            assert len(repeat["instances"]) == 3
            template = [expected[s] for edge in repeat["source"] for s in edge_source[edge]["controls"]]
            for occurrence in repeat["instances"]:
                points = [expected[s] for edge in occurrence for s in edge_source[edge]["controls"]]
                translations = {(p[0] - q[0], p[1] - q[1]) for p, q in zip(points, template)}
                assert len(translations) == 1, "Every control must share the occurrence translation"
                checks += 1
            final = run["rungs"][-1]["constraints"][-1]
            assert final["status"] == ("failed" if program["id"].endswith("outside-trial") else "satisfied")
        if program["family"] == "partition-star":
            circular = program["circular"][0]
            final_edges = [edge_source[e] for e in program["rungs"][-1]["visible"]]
            assert len(final_edges) == len(circular["ring"])
            assert all(edge["controls"][-1] == final_edges[(i + 1) % len(final_edges)]["controls"][0] for i, edge in enumerate(final_edges))
        summaries.append({"id": program["id"], "rungs": len(program["rungs"]), "sites": len(expected),
                          "finiteCoordinateRemainders": len(rounding),
                          "finalConditions": [{"label": c["label"], "status": c["status"]} for c in run["rungs"][-1]["constraints"]]})
    left, _, _ = evaluate(next(p for p in programs if p["id"] == "crescent-left"))
    right, _, _ = evaluate(next(p for p in programs if p["id"] == "crescent-right"))
    # Finite displacement truncation preserves the mirrored frame exactly here.
    for site in ("top-tip", "bottom-tip", "outer-top-control", "inner-top-control"):
        assert left[site][0] + right[site][0] == 512 and left[site][1] == right[site][1]
        checks += 1
    transition_file = root / "transitions.ndjson"
    transition_file.write_text("".join(json.dumps(row, separators=(",", ":")) + "\n" for row in transitions), encoding="utf-8")
    summary = {"schema": "gojo.construction-source-parity.v1", "passed": True, "checks": checks,
               "runs": len(programs), "families": len({p["family"] for p in programs}),
               "rungs": sum(len(p["rungs"]) for p in programs), "transitions": len(transitions),
               "failedTargetTransitions": sum(t["target"]["outcome"] == "failed-authored-condition" for t in transitions),
               "sourceSha256": hashlib.sha256((root / "programs.ndjson").read_bytes()).hexdigest(),
               "transitionSha256": hashlib.sha256(transition_file.read_bytes()).hexdigest(),
               "contextHasOnlyEarlierSites": True, "trainedModel": False,
               "finiteRead": "Independent Fraction evaluation; truncate each displacement toward zero.",
               "scope": "Authored conditional construction transitions; no claim of human stroke order or inferred intent.",
               "programs": summaries}
    (root / "verification.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: summary[key] for key in ("passed", "checks", "runs", "rungs", "transitions")}))
    return summary


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    verify(parser.parse_args().root)
