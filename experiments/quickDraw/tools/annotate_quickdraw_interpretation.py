"""Add inspectable reads to frozen acquired-drawing construction programs.

This stage does not fit, straighten, join or regenerate any construction.
The original observation and program bytes remain the inputs to every read.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from quickdraw_annotations import annotate


def read_rows(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def build(root):
    root = Path(root)
    observation_path, program_path = root/"observations.ndjson", root/"programs.ndjson"
    before = {"observationSha256": digest(observation_path), "programSha256": digest(program_path),
              "sourceSha256": digest(root/"selected.ndjson")}
    observations = {o["sourceId"]: o for o in read_rows(observation_path)}
    programs = read_rows(program_path)
    source_lines = {}
    for line in (root/"selected.ndjson").read_text(encoding="utf-8-sig").splitlines():
        if line.strip():
            source = json.loads(line)
            source_lines[str(source["key_id"])] = (source, line)
    annotations, packets = [], []
    for program in programs:
        source_id = program["sourceMapping"]["sourceId"]
        mode = program["observationMapping"]["mode"]
        observation = observations[source_id]
        annotation = annotate(observation, mode)
        annotation["programId"] = program["id"]
        annotation["sourceId"] = source_id
        annotations.append(annotation)
        source, original = source_lines[source_id]
        packets.append({"source": source, "sourceOriginalJson": original,
                        "observation": observation, "program": program, "annotations": annotation})
    for name, rows in (("annotations.ndjson", annotations), ("annotation-packets.ndjson", packets)):
        (root/name).write_text("".join(json.dumps(row, separators=(",", ":"), ensure_ascii=False)+"\n" for row in rows), encoding="utf-8")
    assert before["observationSha256"] == digest(observation_path)
    assert before["programSha256"] == digest(program_path)
    manifest = {"schema": "gojo.quickdraw-annotation-study.v1", **before,
                "annotationsSha256": digest(root/"annotations.ndjson"),
                "annotationPacketsSha256": digest(root/"annotation-packets.ndjson"),
                "programs": len(programs), "sourceDrawings": len(observations),
                "constructionUnchanged": True,
                "basis": "Measured acquired-source finite fits, with declared per-read tolerance. Mapped native proposal edges are corresponding participants, not an inferred physical join.",
                "status": "Exploratory annotation reads; thresholds and expectations are authored, not learned probabilities.",
                "runs": [{"programId": a["programId"], "sourceId": a["sourceId"],
                          "mode": a["mode"], "counts": a["counts"]} for a in annotations]}
    (root/"annotation-manifest.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    return manifest


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    args = parser.parse_args()
    result = build(args.root)
    print(json.dumps({key: result[key] for key in ("programs", "sourceDrawings", "constructionUnchanged")}))
