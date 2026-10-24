"""Package source evidence beside restored native Gestalt simplification passes.

The manifest supplies correspondence and decision descriptions, never geometry.
All proposed lines, curves, replacement edges and retained guides are captured
reads of the saved ConstructionLadder program. This adapter executes no recipe.
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
from pathlib import Path

from inspect_construction_ladders import load_exports, normalize
from quickdraw_annotations import annotate


def rows(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def build(observations_path, manifest_path, native_path, output, portable=False):
    observations = rows(observations_path)
    observed = {row["sourceId"]: row for row in observations}
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    records = manifest["sources"]
    by_program = {row["programId"]: row for row in records}
    if len(observed) != len(observations) or len(by_program) != len(records):
        raise ValueError("Source and native program identities must be unique")
    if {row["sourceId"] for row in records} != set(observed) or len(records) != len(observed):
        raise ValueError("Each acquired drawing needs exactly one Gestalt program")
    exports = load_exports([native_path])
    if {run["id"] for run, _ in exports} != set(by_program):
        raise ValueError("The manifest must cover exactly the restored native programs")
    sources, copies = [], []
    for run, archive in exports:
        record = by_program[run["id"]]
        observation = observed[record["sourceId"]]
        construction = normalize(run)
        passes = record["passes"]
        if len(passes) != 4 or len(construction["rungs"]) != 4:
            raise ValueError("The experiment requires a baseline and three native refinement passes")
        edges = {edge["id"]: edge for edge in construction["analysis"]["edges"]}
        sites = {site["id"]: site for site in construction["analysis"]["sites"]}
        spans = {span["id"] for span in observation["spans"]}
        mapping = record.get("sourceMapping", {})
        for stage in mapping.get("passes", []):
            if not 0 <= stage["rung"] < len(passes):
                raise ValueError("Source correspondence names an absent native pass")
            for link in stage["links"]:
                if not set(link["spanIds"]) <= spans or not set(link["edgeIds"]) <= edges.keys():
                    raise ValueError("Source correspondence must identify actual intervals and native edges")
                if any(edges[key]["at"] > stage["rung"] for key in link["edgeIds"]):
                    raise ValueError("Source correspondence cannot reference future native geometry")
        all_operation_ids = set()
        for index, (stage, rung) in enumerate(zip(passes, construction["rungs"])):
            if stage["rungId"] != rung["id"]:
                raise ValueError("Pass descriptions must follow native rung order")
            if stage.get("metrics", {}).get("edgeCount", len(rung["visibleEdgeIds"])) != len(rung["visibleEdgeIds"]):
                raise ValueError("Reported edge count differs from captured native visibility")
            stage.setdefault("metrics", {})["edgeCount"] = len(rung["visibleEdgeIds"])
            stage.setdefault("roles", [])
            stage.setdefault("unsupported", [])
            stage.setdefault("operations", [])
            stage.setdefault("residueEdgeIds", [])
            if not set(stage["residueEdgeIds"]) <= set(rung["guideEdgeIds"]):
                raise ValueError("Active residue detail must be retained as actual native guides")
            produced_this_pass = set()
            for operation in stage["operations"]:
                if operation["id"] in all_operation_ids or operation["status"] not in ("accepted", "rejected"):
                    raise ValueError("Every decision requires a unique identity and explicit accepted/rejected state")
                all_operation_ids.add(operation["id"])
                inputs = operation["inputEdgeIds"]
                replacements = operation["replacementEdgeIds"]
                if not inputs or not set(inputs) <= edges.keys() or any(edges[key]["at"] > index for key in inputs):
                    raise ValueError("A simplification decision cannot name future native inputs")
                same_pass = {key for key in inputs if edges[key]["at"] == index}
                if not same_pass <= produced_this_pass:
                    raise ValueError("A same-pass input must have been produced by an earlier recorded operation")
                if not set(replacements) <= edges.keys() or any(edges[key]["at"] != index for key in replacements):
                    raise ValueError("Replacement edges must be introduced in the corresponding native pass")
                if operation["status"] == "rejected" and replacements:
                    raise ValueError("Rejected proposals must not masquerade as executed replacements")
                if operation["status"] == "accepted" and same_pass and replacements:
                    consumed_sites = {site for dependency in rung["dependencies"]
                                      if set(dependency["targetEdgeIds"]) & set(replacements)
                                      for site in dependency["sourceSiteIds"]}
                    consumed_sites.update(site for key in replacements for site in edges[key]["controlSiteIds"])
                    # Primitive frames can consume a retained input through
                    # several actual native sites. Follow those exposed native
                    # references; coordinate resemblance supplies no ancestry.
                    pending = list(consumed_sites)
                    while pending:
                        site = pending.pop()
                        for dependency in sites[site].get("dependencies", []):
                            if dependency not in sites:
                                raise ValueError("A native site dependency is unavailable")
                            if dependency not in consumed_sites:
                                consumed_sites.add(dependency)
                                pending.append(dependency)
                    if any(not consumed_sites.intersection(edges[key]["controlSiteIds"]) for key in same_pass):
                        raise ValueError("Same-pass geometry needs actual consumed native input sites")
                if not operation["sourceSpanIds"] or not set(operation["sourceSpanIds"]) <= spans:
                    raise ValueError("Each decision needs actual acquired interval support")
                if operation["status"] == "accepted":
                    produced_this_pass.update(replacements)
            for group in ("roles", "unsupported"):
                for item in stage[group]:
                    item.setdefault("sourceSpanIds", [])
                    item.setdefault("edgeIds", [])
                    if not set(item["sourceSpanIds"]) <= spans or not set(item["edgeIds"]) <= edges.keys():
                        raise ValueError("Role and remainder descriptions must retain actual participants")
                    if any(edges[key]["at"] > index for key in item["edgeIds"]):
                        raise ValueError("Role hints cannot cite future native geometry")
        filename = run.get("programArchive", run["archive"])
        if Path(filename).name != filename or "/" in filename or "\\" in filename:
            raise ValueError("The native program must be a sibling archive")
        archive = archive.parent / filename
        if not archive.is_file():
            raise ValueError("The native executable program is missing")
        if portable:
            relative = f"archives/{run['id']}.gojo.json.gz"
            construction["archivePath"] = relative
            copies.append((archive, relative))
        else:
            construction["archivePath"] = archive.resolve().as_uri()
        sources.append({"id": record["sourceId"], "label": record.get("label", run["title"]),
                        "observation": observation, "construction": construction, "passes": passes,
                        "annotations": annotate(observation, "gentle-priors"),
                        "policy": record.get("policy", {}),
                        "sourceMapping": record.get("sourceMapping", {})})
    data = {"schema": "gojo.quickdraw-gestalt-viewer.v1", "sources": sources,
            "policy": manifest.get("policy", {}),
            "sourceManifestSha256": hashlib.sha256(manifest_path.read_bytes()).hexdigest(),
            "qualification": "Exploratory rule-guided simplification. The saved native program owns geometry and earlier references; manifest decisions and source annotations are inspectable adapter reads."}
    encoded = json.dumps(data, ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c").replace("\u2028", "\\u2028").replace("\u2029", "\\u2029")
    template = Path(__file__).with_name("quickdraw_gestalt_viewer.html").read_text(encoding="utf-8")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(template.replace("__QUICKDRAW_GESTALT_DATA__", encoded), encoding="utf-8")
    for archive, relative in copies:
        destination = output.parent / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(gzip.compress(archive.read_bytes(), mtime=0))
    if portable:
        (output.parent / "public-files.json").write_text(json.dumps({"files": [output.name, *[relative for _, relative in copies]]}, indent=2)+"\n", encoding="utf-8")
    return {"drawings": len(sources), "programs": len(exports), "passes": sum(len(source["passes"]) for source in sources),
            "htmlBytes": output.stat().st_size, "portable": portable}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--observations", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--native", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--portable", action="store_true")
    args = parser.parse_args()
    print(json.dumps(build(args.observations, args.manifest, args.native, args.output, args.portable), indent=2))


if __name__ == "__main__":
    main()
