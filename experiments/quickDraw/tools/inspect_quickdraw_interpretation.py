"""Local read-only comparison of acquired strokes and native construction reads.

Source observations supply the left panel and support correspondence only.
The right panel is populated exclusively from restored/replayed native exports.
Neither this packager nor its browser viewer executes a drawing recipe.
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import math
from collections import OrderedDict
from pathlib import Path

from inspect_construction_ladders import load_exports, normalize
from quickdraw_annotations import annotate


MODES = ("observed-fit", "gentle-priors", "stronger-priors")


def read_rows(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def build(observations_path, programs_path, native_path, output, portable=False, verification_path=None,
          annotation_native_path=None):
    observations = read_rows(observations_path)
    programs = read_rows(programs_path)
    by_source = {row["sourceId"]: row for row in observations}
    by_program = {row["id"]: row for row in programs}
    if len(by_source) != len(observations) or len(by_program) != len(programs):
        raise ValueError("Duplicate source or construction identity")
    annotation_archives = {}
    if annotation_native_path:
        summaries = json.loads(annotation_native_path.read_text(encoding="utf-8-sig"))
        annotation_archives = {row["id"]: row for row in summaries}
        if len(annotation_archives) != len(summaries) or set(annotation_archives) != set(by_program):
            raise ValueError("Native annotation archives must cover every displayed program exactly once")
    verification_path = verification_path or observations_path.parent / "verification.json"
    verified_runs = {}
    if verification_path.is_file():
        verification = json.loads(verification_path.read_text(encoding="utf-8"))
        if verification.get("passed") is not True:
            raise ValueError("Approximation audit has not passed")
        for file, key in ((observations_path, "observationSha256"), (programs_path, "programSha256")):
            if verification.get(key) != hashlib.sha256(file.read_bytes()).hexdigest():
                raise ValueError("Approximation audit is stale for this source/program correspondence")
        verified_runs = {row["program"]: row for row in verification["runs"]}
        if set(verified_runs) != set(by_program):
            raise ValueError("Approximation audit does not cover every displayed native program")
    sources = OrderedDict()
    archive_copies = []
    exports = load_exports([native_path])
    if {run["id"] for run, _ in exports} != set(by_program):
        raise ValueError("Every source program needs exactly one native replay export")
    for run, archive in exports:
        program = by_program[run["id"]]
        mapping = program["observationMapping"]
        source_id = mapping["sourceId"]
        if source_id not in by_source or mapping["mode"] not in MODES:
            raise ValueError("Unknown observation source or interpretation mode")
        observed = by_source[source_id]
        variant = normalize(run)
        native_edges = {edge["id"] for edge in variant["analysis"]["edges"]}
        source_spans = {span["id"] for span in observed["spans"]}
        if set(mapping["spanEdges"]) != source_spans:
            raise ValueError("Source correspondence must cover every observed interval")
        expected_inverse = {}
        for span_id, edges in mapping["spanEdges"].items():
            if not edges or len(set(edges)) != len(edges) or not set(edges) <= native_edges:
                raise ValueError("A source interval names absent or duplicate native edges")
            for edge_id in edges:
                expected_inverse.setdefault(edge_id, []).append(span_id)
        if {key: sorted(value) for key, value in mapping["edgeSpans"].items()} != {
                key: sorted(value) for key, value in expected_inverse.items()}:
            raise ValueError("Source interval correspondence must agree in both directions")
        if not set(expected_inverse) <= set(variant["rungs"][-1]["visibleEdgeIds"]):
            raise ValueError("Final native read does not retain all mapped source intervals")
        variant["observationMapping"] = mapping
        variant["mode"] = mapping["mode"]
        variant["approximationAudit"] = verified_runs.get(run["id"])
        native_by_id = {edge["id"]: edge for edge in variant["analysis"]["edges"]}
        spans_by_id = {span["id"]: span for span in observed["spans"]}
        variant["edgeComparison"] = {}
        for edge_id, span_ids in mapping["edgeSpans"].items():
            if len(span_ids) == 1:
                before, after = spans_by_id[span_ids[0]]["controls"], native_by_id[edge_id]["controls"]
                if len(before) == len(after):
                    variant["edgeComparison"][edge_id] = {"maxControlShift": max(math.dist(a, b) for a, b in zip(before, after)),
                                                           "basis": "512-unit normalized page"}
        # If available, these measured adapter notes explain deliberate changes;
        # no proposal coordinates are taken from them.
        variant["interpretation"] = program.get("interpretationPolicy", program.get("interpretation", {}))
        # Annotation reads select expectations over acquired evidence and the
        # corresponding saved proposal. They never modify the executed geometry.
        geometry_before = json.dumps(variant["analysis"], sort_keys=True, separators=(",", ":"))
        variant["annotations"] = annotate(observed, mapping["mode"], variant)
        variant["annotations"]["programId"] = run["id"]
        if json.dumps(variant["analysis"], sort_keys=True, separators=(",", ":")) != geometry_before:
            raise ValueError("Annotation reads must not change captured native geometry")
        for group in ("lines", "joints", "symmetries", "relations"):
            for candidate in variant["annotations"][group]:
                if not candidate["spans"] or not set(candidate["spans"]) <= source_spans:
                    raise ValueError("An annotation must name actual acquired support intervals")
        if annotation_native_path:
            summary = annotation_archives[run["id"]]
            if summary.get("sourceId") != source_id or summary.get("mode") != mapping["mode"]:
                raise ValueError("Native annotation identity does not match the displayed source and mode")
            if not all(summary.get(key) is True for key in ("nativeRestoreVerified", "actualSourceAliasesVerified",
                       "nativeFiniteResidualsVerified", "targetGeometryUnchanged", "sourceOrderRetained")):
                raise ValueError("Native annotation archive has not passed its structural checks")
            for group, key in (("lines", "lineReads"), ("joints", "jointReads"),
                               ("symmetries", "reflectionReads"), ("relations", "selectedRelationReads")):
                if summary.get(key) != len(variant["annotations"][group]):
                    raise ValueError("Native annotation archive selections differ from the displayed annotation read")
            annotation_name = summary["archive"]
            if Path(annotation_name).name != annotation_name or "/" in annotation_name or "\\" in annotation_name:
                raise ValueError("Native annotation must be a sibling archive")
            annotation_archive = annotation_native_path.parent / annotation_name
            if not annotation_archive.is_file():
                raise ValueError("Native annotation archive is missing")
            if portable:
                relative = f"annotation-archives/{run['id']}.annotations.gojo.json.gz"
                variant["annotationArchivePath"] = relative
                archive_copies.append((annotation_archive, relative))
            else:
                variant["annotationArchivePath"] = annotation_archive.resolve().as_uri()
        name = run.get("programArchive", run["archive"])
        if Path(name).name != name or "/" in name or "\\" in name:
            raise ValueError("Native program must be a sibling archive")
        archive = archive.parent / name
        if not archive.is_file():
            raise ValueError("Native program archive is missing")
        if portable:
            relative = f"archives/{run['id']}.gojo.json.gz"
            variant["archivePath"] = relative
            archive_copies.append((archive, relative))
        else:
            variant["archivePath"] = archive.resolve().as_uri()
        source = sources.setdefault(source_id, {"id": source_id,
            "label": program.get("title", f"Drawing {len(sources)+1}"),
            "observation": observed, "variants": []})
        source["variants"].append(variant)
    if set(sources) != set(by_source):
        raise ValueError("An acquired drawing has no native interpretation")
    for source in sources.values():
        if sorted(v["mode"] for v in source["variants"]) != sorted(MODES):
            raise ValueError("Every drawing needs the three distinct interpretation reads")
        source["variants"].sort(key=lambda row: MODES.index(row["mode"]))
        if len({len(v["rungs"]) for v in source["variants"]}) != 1:
            raise ValueError("Interpretation steps do not correspond")
    data = {"schema": "gojo.quickdraw-interpretation-viewer.v1", "sources": list(sources.values()),
            "qualification": "Five curated drawings; exploratory geometric reads, not a trained church model. Geometry comes from restored native programs. Per-mode annotation leeway selects measured possibilities without changing source identity or saved geometry."}
    encoded = json.dumps(data, ensure_ascii=False, separators=(",", ":"))
    encoded = encoded.replace("<", "\\u003c").replace("\u2028", "\\u2028").replace("\u2029", "\\u2029")
    template = Path(__file__).with_name("quickdraw_interpretation_viewer.html").read_text(encoding="utf-8")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(template.replace("__QUICKDRAW_INTERPRETATION_DATA__", encoded), encoding="utf-8")
    for archive, relative in archive_copies:
        target = output.parent / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(gzip.compress(archive.read_bytes(), mtime=0))
    if portable:
        (output.parent / "public-files.json").write_text(json.dumps({"files": [output.name, *[r for _, r in archive_copies]]}, indent=2)+"\n", encoding="utf-8")
    return {"drawings": len(sources), "programs": len(exports),
            "rungs": sum(len(v["rungs"]) for source in sources.values() for v in source["variants"]),
            "sourceSamples": sum(row["counts"]["samples"] for row in observations),
            "htmlBytes": output.stat().st_size, "portable": portable}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--observations", type=Path, required=True)
    parser.add_argument("--programs", type=Path, required=True)
    parser.add_argument("--native", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--portable", action="store_true", help="Prepare a standalone review folder; does not publish.")
    parser.add_argument("--verification", type=Path, help="Passed source/program audit with final proposal error; defaults to the observations' sibling verification.json")
    parser.add_argument("--annotation-native", type=Path, help="Verified native annotation summary.json, for optional archive downloads")
    args = parser.parse_args()
    print(json.dumps(build(args.observations, args.programs, args.native, args.output, args.portable,
                           args.verification, args.annotation_native), indent=2))


if __name__ == "__main__":
    main()
