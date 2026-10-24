"""Retain an exact, compressed GitHub checkpoint of a verified local study."""
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
from pathlib import Path


def retain(source, destination):
    verification = json.loads((source/"verification.json").read_text(encoding="utf-8"))
    if not verification["passed"]:
        raise ValueError("Only a verified experiment may become a checkpoint")
    for name, key in (("selected.ndjson", "sourceSha256"), ("programs.ndjson", "programSha256")):
        if hashlib.sha256((source/name).read_bytes()).hexdigest() != verification[key]:
            raise ValueError("Experiment changed after verification: "+name)
    manifest_hash = hashlib.sha256((source/"manifest.json").read_bytes()).hexdigest()
    if manifest_hash not in (source/"review/index.html").read_text(encoding="utf-8"):
        raise ValueError("Portable review was built from another manifest")
    destination.mkdir(parents=True, exist_ok=True)

    def copy(original, relative, compress=False):
        target = destination/relative
        target.parent.mkdir(parents=True, exist_ok=True)
        raw = original.read_bytes()
        target.write_bytes(gzip.compress(raw, mtime=0) if compress else raw)

    for name in ("observations.ndjson", "proposals.ndjson", "programs.ndjson", "packets.ndjson"):
        copy(source/name, name+".gz", True)
    for name in ("selected.ndjson", "manifest.json", "verification.json"):
        copy(source/name, name)
    copy(source/"native/summary.json", "native-summary.json")
    copy(source/"native/inspector.ndjson", "native-inspector.ndjson.gz", True)
    copy(source/"native-evidence/summary.json", "native-evidence-summary.json")
    for original in sorted((source/"native").glob("*.construction.gojo.json")):
        copy(original, "native/"+original.name+".gz", True)
    for original in sorted((source/"native-evidence").glob("*.gestalt.gojo.json")):
        copy(original, "native-evidence/"+original.name+".gz", True)
    for original in sorted((source/"review").rglob("*")):
        if original.is_file():
            copy(original, original.relative_to(source))
    for name in ("qa-final", "qa-portable"):
        folder = source/name
        if not folder.is_dir():
            raise ValueError("Both final browser checks are required: "+str(folder))
        report = json.loads((folder/"browser-summary.json").read_text(encoding="utf-8"))
        if report.get("consoleErrors") or report.get("externalRequests") or report.get("mobileOverflow") or not report.get("capturedGeometryExact"):
            raise ValueError("Browser verification reported errors")
        for original in sorted(folder.rglob("*")):
            if original.is_file():
                copy(original, original.relative_to(source))
    (destination/".gitattributes").write_text("# Preserve exact acquisition bytes, native archives and captured review hashes.\n* -text -whitespace\n", encoding="utf-8")
    proposals = [json.loads(line) for line in (source/"proposals.ndjson").read_text(encoding="utf-8").splitlines() if line]
    summary = {"schema": "gojo.quickdraw-gestalt-checkpoint.v1", "drawings": verification["drawings"], "rungs": verification["rungs"],
        "sourceSha256": verification["sourceSha256"], "policy": proposals[0]["policy"],
        "results": [{"sourceId": row["sourceId"], "mainProfileCounts": [step["metrics"]["edgeCount"] for step in row["steps"]],
            "symmetricRms": [step["metrics"]["rms"] for step in row["steps"]], "retainedDetailProfiles": row["steps"][-1]["metrics"]["residueEdgeCount"],
            "accepted": row["convergence"]["accepted"], "rejected": sum(step["convergence"]["rejected"] for step in row["steps"])} for row in proposals],
        "publication": "Local review and GitHub checkpoint; no website publication in this milestone.",
        "qualification": "Authored rule-guided conceptual approximation; source difference may intentionally increase. No learned intent, nonnumeric completion or global optimum is claimed."}
    if any("completionPolicy" in row for row in proposals):
        summary["schema"] = "gojo.quickdraw-synthesis-checkpoint.v1"
        summary["completionPolicy"] = proposals[0]["completionPolicy"]
        for result, row in zip(summary["results"], proposals):
            result["viewHypothesis"] = row["steps"][-1].get("viewHypothesis", {})
            result["lateStageWeights"] = [step.get("policyWeights", {}) for step in row["steps"][4:]]
    (destination/"checkpoint-summary.json").write_text(json.dumps(summary, indent=2)+"\n", encoding="utf-8")
    inventory = [{"path": str(path.relative_to(destination)).replace("\\", "/"), "bytes": path.stat().st_size, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
        for path in sorted(destination.rglob("*")) if path.is_file() and path.name != "file-manifest.json"]
    (destination/"file-manifest.json").write_text(json.dumps(inventory, indent=2)+"\n", encoding="utf-8")
    return {"files": len(inventory)+1, "bytes": sum(item["bytes"] for item in inventory), "destination": str(destination)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--destination", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(retain(args.source, args.destination), indent=2))
