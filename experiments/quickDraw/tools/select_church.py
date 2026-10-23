"""Reproducible visual church acquisition fixture; no category learning target.

Candidate records and chosen source bytes retain all coordinates, stroke/sample
order, time arrays and metadata. Only drawing arrays enter the frozen encoder.
Review labels/notes are audit context, not ideal templates or model features.
"""
from __future__ import annotations

import argparse
import hashlib
import html
import json
from pathlib import Path

import select_samples


# Candidate index, exact source ID, visual note. Filled only after image review,
# before fitting; this list never depends on reconstruction success.
REVIEW = [
    (14, "5743197026254848", "Simple frontal roof/body and cross; bowed edges, uneven proportions and open roof joins remain visible."),
    (18, "5181133614481408", "Oblique building with roof and side wall, cross and doorway; skewed perspective and separate detail strokes retained."),
    (22, "5437095780810752", "Tall gabled front with circular clock-like detail, doorway and side wing; seventeen acquired strokes exercise a high stroke floor."),
    (23, "6169132418465792", "Three rounded tower profiles with crosses and an arched doorway; slant, gaps and unequal repeated curves retained."),
    (33, "4578701515685888", "Stepped central tower and cross over a broad body with arched door; retraced top, open boundaries and multiple pen lifts retained."),
]


def candidates(args):
    select_samples.CATEGORIES = ("church",)
    select_samples.candidates(args)


def select(args):
    folder = args.output / "church"
    inventory = json.loads((args.output / "inventory.json").read_text(encoding="utf-8"))
    source = Path(inventory["sources"][0]["path"])
    manifest = json.loads((folder / "candidates.manifest.json").read_text(encoding="utf-8"))
    lines = (folder / "candidates.ndjson").read_bytes().splitlines(keepends=True)
    by_index = {row["candidate_index"]: (row, raw) for row, raw in zip(manifest, lines, strict=True)}
    chosen = []
    for index, expected_id, note in REVIEW:
        row, raw = by_index[index]
        if row["key_id"] != expected_id or hashlib.sha256(raw).hexdigest() != row["record_sha256"]:
            raise ValueError("Candidate differs from visual review; restore default seed/prefix and re-review")
        with source.open("rb") as stream:
            stream.seek(row["byte_offset"])
            if stream.read(row["byte_length"]) != raw:
                raise ValueError("Candidate source bytes changed")
        chosen.append(row | {"record": json.loads(raw), "raw": raw,
                             "visual_review_note": note, "source_path": str(source.resolve())})
    if len(chosen) != 5 or len({r["key_id"] for r in chosen}) != 5:
        raise ValueError("Church fixture must have five distinct visually reviewed sources")
    destination = args.output.parent
    raw_bytes = b"".join(row["raw"] for row in chosen)
    (destination / "selected.ndjson").write_bytes(raw_bytes)
    with (destination / "drawings-only.ndjson").open("w", encoding="utf-8", newline="\n") as stream:
        for row in chosen:
            stream.write(json.dumps({"drawing": row["record"]["drawing"]}, separators=(",", ":")) + "\n")
    selection = {
        "schema": "quickdraw-church-visual-selection/1",
        "status": "visually reviewed before fitting; frozen-model development fixture",
        "scope": "five varied recognizable church drawings; curated, not prevalence representative",
        "category_role": "acquisition/review stratum only, no context or category input to model",
        "selection_policy": f'{inventory["sources"][0]["candidate_count"]} deterministic encoding-valid candidates from first '
                            f'{inventory["sources"][0]["prefix_records_read"]} records, then visual review',
        "selected_ndjson_sha256": hashlib.sha256(raw_bytes).hexdigest(),
        "drawings_only_sha256": hashlib.sha256((destination / "drawings-only.ndjson").read_bytes()).hexdigest(),
        "source_inventory": str((args.output / "inventory.json").resolve()),
        "source": inventory["sources"][0],
        "chronology": "exact source stroke/sample order and time arrays; never sorted by timestamp",
        "samples": [{"record_index": i, **{k: v for k, v in row.items() if k not in ("record", "raw")}}
                    for i, row in enumerate(chosen)],
    }
    select_samples.write_json(destination / "selection-manifest.json", selection)
    select_samples.render_sheet(chosen, destination / "selected-overview.png",
        "Quick Draw | five church source drawings",
        "Original orientation and aspect; imperfections, details and recorded stroke order retained", columns=5)
    table = ''.join(f'<tr><td>{row["candidate_index"]:02d}</td><td>{row["key_id"]}</td>'
                    f'<td>{html.escape(row["visual_review_note"])}</td></tr>' for row in chosen)
    (destination / "selection-review.html").write_text(
        '<!doctype html><meta charset="utf-8"><title>Church source selection</title>'
        '<style>body{font:16px/1.5 system-ui;margin:32px;color:#213747}img{max-width:100%}'
        'td{padding:8px;border-bottom:1px solid #ddd}table{border-collapse:collapse}</style>'
        '<h1>Five church drawings</h1><p>Selected before fitting from 36 deterministic candidates. '
        'This is a curated structural transfer fixture, not a representative survey. '
        'Labels and notes are review context only; the frozen receiver gets no church template.</p>'
        '<img src="selected-overview.png" alt="Five original church drawings"><table>' + table + '</table>'
        '<p><a href="candidates/church/candidates.png">All candidate drawings</a> | '
        '<a href="selected.ndjson">Exact original records</a> | '
        '<a href="selection-manifest.json">Selection and source hashes</a></p>'
        '<p>Source: Google Creative Lab, '
        '<a href="https://github.com/googlecreativelab/quickdraw-dataset">Quick, Draw! dataset</a>, '
        '<a href="https://creativecommons.org/licenses/by/4.0/">CC BY 4.0</a>.</p>', encoding="utf-8")
    print(json.dumps({"output": str(destination), "selected": len(chosen),
                      "strokes": sum(r["metrics"]["stroke_count"] for r in chosen),
                      "points": sum(r["metrics"]["point_count"] for r in chosen),
                      "sha256": selection["selected_ndjson_sha256"]}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("candidates", "select"))
    parser.add_argument("--source", type=Path, default=Path("D:/_quickDraw/quickdraw"))
    parser.add_argument("--output", type=Path, default=Path("D:/_quickDraw/gojo-reconstruction/church/candidates"))
    parser.add_argument("--prefix", type=int, default=2000)
    parser.add_argument("--count", type=int, default=36)
    parser.add_argument("--seed", default="20261023-church")
    args = parser.parse_args()
    if args.prefix <= 0 or not 0 < args.count <= args.prefix:
        parser.error("require 0 < count <= prefix")
    (candidates if args.mode == "candidates" else select)(args)


if __name__ == "__main__":
    main()
