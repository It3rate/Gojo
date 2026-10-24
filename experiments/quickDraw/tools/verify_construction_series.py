"""Independently audit source correspondence and causal drawing constructions.

The authored sources are reloaded separately from each construction's declared
mapping. Coordinate calculations use Fraction and the published integer law;
the construction helpers and their evaluator are deliberately not imported.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from fractions import Fraction
from pathlib import Path


class VerificationError(ValueError):
    """A source, construction or restored read violates the selected contract."""


def canonical_sha256(value):
    encoded = json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def read_lines(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def load_sources(directory=None):
    """Reload existing artwork, including the pinned centerline imports."""
    import authored_line_art
    from import_line_art_svg import import_svg

    scenes = authored_line_art.church_scenes() + authored_line_art.diagnostics()
    directory = directory or Path(__file__).resolve().parents[1] / "sources" / "line-art"
    manifest = json.loads((directory / "manifest.json").read_text(encoding="utf-8"))
    for collection in ("lucide", "tabler"):
        item = next(row for row in manifest["items"]
                    if row["collection"] == collection and row["role"] == "asset")
        data = (directory / item["file"]).read_bytes()
        if hashlib.sha256(data).hexdigest() != item["sha256"]:
            raise VerificationError(f"Pinned source hash changed: {collection}")
        provenance = {key: item[key] for key in
                      ("collection", "sourceUrl", "sourcePage", "license", "attribution", "revision")}
        scenes.append(import_svg(data, collection + "-church", provenance))
    return {scene["id"]: scene for scene in scenes}


class Audit:
    def __init__(self, label):
        self.label = label
        self.checks = 0

    def expect(self, actual, expected, message):
        self.checks += 1
        if actual != expected:
            raise VerificationError(f"{self.label}: {message}; {actual!r} != {expected!r}")

    def require(self, condition, message):
        self.expect(bool(condition), True, message)


def evaluate(program, audit=None):
    """Read site instructions without using the construction implementation."""
    audit = audit or Audit(program["id"])
    parameters = {row["id"]: row["value"] for row in program["parameters"]}
    audit.expect(len(parameters), len(program["parameters"]), "parameter identities are unique")
    values, dependencies, at, remainders = {}, {}, {}, []
    for site in program["sites"]:
        key = site["id"]
        audit.require(key not in values, "site identities are unique")
        audit.require(0 <= site["at"] < len(program["rungs"]), f"{key} has an available rung")
        coordinates, refs = [], set()
        for axis, name in enumerate(("x", "y")):
            expression = site[name]
            if isinstance(expression, int) and not isinstance(expression, bool):
                coordinates.append(expression)
                continue
            audit.require(isinstance(expression, dict), f"{key}.{name} has a finite coordinate instruction")
            first, second = expression["from"], expression["to"]
            origin = expression.get("origin", first)
            for source in (origin, first, second):
                audit.require(source in values, f"{key} cannot reference an unavailable or later site {source}")
                audit.require(at[source] <= site["at"], f"{key} cannot reference a future rung {source}")
                refs.add(source)
            numerator = expression["numerator"]
            if isinstance(numerator, str):
                audit.require(numerator in parameters, f"{key} references an available parameter")
                numerator = parameters[numerator]
            denominator = expression["denominator"]
            audit.require(isinstance(numerator, int) and not isinstance(numerator, bool),
                          f"{key} has an integer sample")
            audit.require(isinstance(denominator, int) and not isinstance(denominator, bool) and denominator > 0,
                          f"{key} has a positive integer grade")
            displacement = Fraction((values[second][axis] - values[first][axis]) * numerator, denominator)
            whole = int(displacement)  # Fraction int conversion truncates toward zero.
            coordinates.append(values[origin][axis] + whole)
            if displacement.denominator != 1:
                remainder = displacement - whole
                remainders.append({"site": key, "axis": name,
                                   "coordinateRemainder": [remainder.numerator, remainder.denominator]})
        values[key], dependencies[key], at[key] = coordinates, refs, site["at"]
    return values, dependencies, remainders


def verify_program(program, run, source):
    audit = Audit(program["id"])
    audit.expect(run["id"], program["id"], "restored program identity")
    audit.expect(run["nativeRestoreVerified"], True, "native restoration is verified")
    audit.expect(run["nativeReplayVerified"], True, "native replay is verified")
    audit.expect(run["viewBox"], program["viewBox"], "presentation frame is retained")
    audit.expect(len(run["rungs"]), len(program["rungs"]), "all rungs are restored")
    values, deps, rounding = evaluate(program, audit)
    sites = {site["id"]: site for site in program["sites"]}
    edges = {edge["id"]: edge for edge in program["edges"]}
    audit.expect(len(edges), len(program["edges"]), "edge identities are unique")
    for key, edge in edges.items():
        audit.require(2 <= len(edge["controls"]) <= 4, f"{key} has supported ordered controls")
        audit.require(all(control in sites for control in edge["controls"]), f"{key} uses existing sites")
        audit.require(all(sites[control]["at"] <= edge["at"] for control in edge["controls"]),
                      f"{key} consumes only available control sites")
    previous, seen = [], set()
    for index, (plan, restored) in enumerate(zip(program["rungs"], run["rungs"])):
        audit.expect(restored["id"], plan["id"], "rung identities are preserved")
        audit.expect(restored["verifiedNativeRestore"], True, "rung restoration is verified")
        audit.expect(restored["verifiedNativeReplay"], True, "rung replay is verified")
        available = [site["id"] for site in program["sites"] if site["at"] <= index]
        audit.expect([site["id"] for site in restored["sites"]], available,
                     "rung site inventory contains only current and earlier sites")
        for site in restored["sites"]:
            audit.expect([site["x"], site["y"]], values[site["id"]], "independent affine site position")
            audit.expect(set(site["dependencies"]), deps[site["id"]], "actual site dependencies")
            audit.expect(site["at"], sites[site["id"]]["at"], "site introduction rung")
        for scope in ("visible", "guides", "selected", "introduced"):
            audit.expect(len(set(plan[scope])), len(plan[scope]), f"{scope} identities are unique")
            audit.require(set(plan[scope]) <= set(edges), f"{scope} names known edges")
        audit.expect(set(plan["visible"]) & set(plan["guides"]), set(), "guides and result are separate reads")
        audit.expect(set(plan["selected"]), set(previous) - set(plan["visible"]),
                     "retired result edges are selected explicitly")
        introduced = {key for key, edge in edges.items() if edge["at"] == index}
        audit.expect(set(plan["introduced"]), introduced, "every new edge is introduced exactly once")
        seen.update(introduced)
        audit.require(set(plan["visible"] + plan["guides"]) <= seen,
                      "rung cannot display future edges")
        audit.expect([edge["id"] for edge in restored["edges"] if not edge["guide"]], plan["visible"],
                     "native ordered visible edges")
        audit.expect([edge["id"] for edge in restored["edges"] if edge["guide"]], plan["guides"],
                     "native ordered guide edges")
        for edge in restored["edges"]:
            instruction = edges[edge["id"]]
            controls = instruction["controls"]
            audit.expect(edge["controlSiteIds"], controls, "native control identity and order")
            audit.expect(edge["controls"], [values[key] for key in controls], "native curve controls")
            audit.expect(edge["from"], controls[0], "native ordered start identity")
            audit.expect(edge["to"], controls[-1], "native ordered end identity")
            audit.expect(edge["at"], instruction["at"], "native edge introduction rung")
            audit.expect(edge["introduced"], edge["id"] in plan["introduced"], "native introduction read")
        previous = plan["visible"]

    mapping = program["sourceMapping"]
    audit.expect(mapping["status"], "authored-source-correspondence", "mapping is an authored claim")
    audit.expect(mapping["sceneId"], source["id"], "original scene identity")
    audit.expect(mapping["sourceSha256"], canonical_sha256(source), "original scene content hash")
    source_sites = {site["id"]: site for site in source["sites"]}
    source_edges = {edge["id"]: edge for edge in source["edges"]}
    site_map, edge_map = mapping["siteMap"], mapping["edgeMap"]
    audit.require(set(edge_map) <= set(source_edges), "all mapped edges belong to the original source")
    audit.expect(mapping["sourceEdgeOrder"], [edge["id"] for edge in source["edges"] if edge["id"] in edge_map],
                 "original edge ordering is retained separately from authored rung order")
    strokes = [[edge for edge in stroke["edges"] if edge in edge_map] for stroke in source["strokes"]]
    audit.expect(mapping["sourceStrokeOrder"], [stroke for stroke in strokes if stroke],
                 "source acquisition scopes and order are retained")
    expected_sites = {source_edges[key][end] for key in edge_map for end in ("from", "to")}
    audit.expect(set(site_map), expected_sites, "source endpoint identity inventory")
    audit.expect(len(set(site_map.values())), len(site_map), "distinct source sites cannot be welded by coordinate equality")
    audit.expect(len(set(edge_map.values())), len(edge_map), "distinct source edges keep separate identities")
    audit.require(set(site_map.values()) <= set(sites), "all mapped construction sites exist")
    audit.require(set(edge_map.values()) <= set(edges), "all mapped construction edges exist")
    expected_controls = {key: source_edges[key]["controls"] for key in edge_map}
    expected_positions = {key: [source_sites[key]["x"], source_sites[key]["y"]] for key in site_map}
    audit.expect(mapping["expectedSites"], expected_positions, "expected positions agree with independent original source")
    audit.expect(mapping["expectedControls"], expected_controls, "expected controls agree with independent original source")
    for key, target in site_map.items():
        audit.expect(values[target], expected_positions[key], "final endpoint retains source position")
    for key, target in edge_map.items():
        controls = edges[target]["controls"]
        audit.expect(controls[0], site_map[source_edges[key]["from"]], "final start retains source identity")
        audit.expect(controls[-1], site_map[source_edges[key]["to"]], "final end retains source identity")
        audit.expect([values[control] for control in controls], expected_controls[key],
                     "final geometry matches original source controls")
        earlier_controls = {control for plan in program["rungs"][:edges[target]["at"]]
                            for edge_id in plan["visible"] for control in edges[edge_id]["controls"]}
        ancestors, pending = set(), list(controls)
        while pending:
            control = pending.pop()
            if control not in ancestors:
                ancestors.add(control)
                pending.extend(deps[control])
        audit.require(bool(ancestors & earlier_controls),
                      "final source edge consumes actual earlier visible construction sites")
    audit.expect(set(program["rungs"][-1]["visible"]), set(edge_map.values()),
                 "finished result contains every selected source edge exactly once")
    return {"id": program["id"], "sceneId": source["id"], "checks": audit.checks,
            "rungs": len(program["rungs"]), "sites": len(sites), "edges": len(edges),
            "sourceSites": len(site_map), "sourceEdges": len(edge_map),
            "finiteCoordinateRemainders": len(rounding), "sourceSha256": mapping["sourceSha256"]}


def verify_inventory(programs, sources):
    """Check the requested fourteen churches, A–Z, and complete alphabet page."""
    audit = Audit("construction series inventory")
    churches = {f"church-{index:02d}" for index in range(1, 13)} | {"lucide-church", "tabler-church"}
    by_source = {}
    for program in programs:
        mapping = program["sourceMapping"]
        by_source.setdefault(mapping["sceneId"], []).append(program)
    audit.expect(set(by_source), churches | {"diagnostic-alphabet"}, "all selected original source scenes")
    for key in sorted(churches):
        candidates = by_source[key]
        audit.expect(len(candidates), 1, f"{key} has one construction")
        audit.expect(set(candidates[0]["sourceMapping"]["edgeMap"]),
                     {edge["id"] for edge in sources[key]["edges"]}, f"{key} includes every source edge")
    alphabet = sources["diagnostic-alphabet"]
    alphabet_programs = by_source["diagnostic-alphabet"]
    all_edges = {edge["id"] for edge in alphabet["edges"]}
    atlas = [program for program in alphabet_programs if set(program["sourceMapping"]["edgeMap"]) == all_edges]
    audit.expect(len(atlas), 1, "one complete alphabet atlas")
    audit.expect(len(alphabet_programs), 27, "one atlas and twenty-six individual letters")
    letters = [obj for obj in alphabet["objects"] if obj.get("label") in set("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    audit.expect({obj["label"] for obj in letters}, set("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), "A through Z are represented")
    for obj in letters:
        matches = [program for program in alphabet_programs
                   if set(program["sourceMapping"]["edgeMap"]) == set(obj["edges"])]
        audit.expect(len(matches), 1, f"letter {obj['label']} has one complete scoped construction")
    return audit.checks


def verify(root, sources=None):
    sources = load_sources() if sources is None else sources
    programs = [program for program in read_lines(root / "programs.ndjson") if "sourceMapping" in program]
    runs = {run["id"]: run for run in read_lines(root / "native" / "inspector.ndjson")}
    if not programs:
        raise VerificationError("No source-mapped construction programs were selected")
    summaries = [verify_program(program, runs[program["id"]], sources[program["sourceMapping"]["sceneId"]])
                 for program in programs]
    inventory_checks = verify_inventory(programs, sources)
    report = {"schema": "gojo.construction-series-source-parity.v1", "passed": True,
              "programs": len(summaries), "checks": sum(row["checks"] for row in summaries) + inventory_checks,
              "inventoryChecks": inventory_checks, "churches": 14, "individualLetters": 26, "alphabetAtlases": 1,
              "rungs": sum(row["rungs"] for row in summaries),
              "sourceScenes": len({row["sceneId"] for row in summaries}),
              "sourceSha256": hashlib.sha256((root / "programs.ndjson").read_bytes()).hexdigest(),
              "nativeInspectorSha256": hashlib.sha256((root / "native" / "inspector.ndjson").read_bytes()).hexdigest(),
              "auditScriptSha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
              "checksPerformed": ["independent rational coordinate evaluation and integer displacement rounding",
                                  "earlier site availability and rung causality",
                                  "native identity, control order, membership, introduction and retirement",
                                  "final edge ancestry reaches actual earlier visible construction controls",
                                  "original source content hash, edge order and stroke scopes",
                                  "source endpoint identity injectivity and complete final control parity"],
              "limits": ["Construction steps are authored recipes, not recovered human drawing order.",
                         "Source mappings are externally checked correspondence claims; the native program owns executable geometry.",
                         "Rough guides may extend beyond the finished drawing or page; this audit does not clamp or constrain them.",
                         "Exact source parity does not establish learned priors, conceptual recognition or nonnumeric resolution."],
              "runs": summaries}
    (root / "series-verification.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: report[key] for key in ("passed", "programs", "checks", "rungs", "sourceScenes")}))
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    verify(parser.parse_args().root)
