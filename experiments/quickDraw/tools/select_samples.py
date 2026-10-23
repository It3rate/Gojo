"""Bounded Quick Draw acquisition selection and visual review, not recognition.

Run `python select_samples.py candidates` to retain deterministic candidates.
Then use `python select_samples.py select --review path/to/review.json` to retain
visually reviewed IDs. All artifacts go outside the repository by default.
Pillow is needed only for rendering; no dataset-wide scan or downloads occur.
"""

from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import html
import json
import math
import os
from pathlib import Path


CATEGORIES = ("square", "circle", "triangle", "octagon", "moon")


def write_json(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def validate_record(record: dict) -> list[str]:
    """Validate acquisition encoding without interpreting what was drawn."""
    errors = []
    if not isinstance(record.get("key_id"), str):
        errors.append("key_id is not a string")
    drawing = record.get("drawing")
    if not isinstance(drawing, list) or not drawing:
        return errors + ["drawing has no strokes"]
    for i, stroke in enumerate(drawing):
        if not isinstance(stroke, list) or len(stroke) not in (2, 3):
            errors.append(f"stroke {i} does not have x/y or x/y/t arrays")
            continue
        if not all(isinstance(axis, list) for axis in stroke):
            errors.append(f"stroke {i} has a non-array axis")
            continue
        if not stroke[0] or len({len(axis) for axis in stroke}) != 1:
            errors.append(f"stroke {i} has empty or mismatched axes")
        if any(isinstance(value, bool) or not isinstance(value, (float, int))
               or not math.isfinite(value) for axis in stroke for value in axis):
            errors.append(f"stroke {i} has a non-finite/non-numeric sample")
    return errors


def acquisition_metrics(record: dict) -> dict:
    drawing = record["drawing"]
    points = [(x, y) for stroke in drawing for x, y in zip(*stroke[:2])]
    duplicates = sum(sum(x0 == x1 and y0 == y1 for x0, x1, y0, y1 in
                         zip(s[0], s[0][1:], s[1], s[1][1:])) for s in drawing)
    backwards_time = sum(sum(a > b for a, b in zip(s[2], s[2][1:]))
                         for s in drawing if len(s) == 3)
    return {
        "stroke_count": len(drawing),
        "point_count": len(points),
        "stroke_axis_counts": [len(s) for s in drawing],
        "coordinate_bounds": [min(x for x, y in points), min(y for x, y in points),
                              max(x for x, y in points), max(y for x, y in points)],
        "has_fractional_coordinates": any(v != int(v) for p in points for v in p),
        "consecutive_duplicate_xy_count": duplicates,
        "within_stroke_backwards_time_count": backwards_time,
    }


def render_sheet(rows: list[dict], path: Path, title: str, subtitle: str,
                 columns: int = 6) -> None:
    from PIL import Image, ImageDraw, ImageFont

    cell_w, cell_h, top = 285, 270, 95
    image = Image.new("RGB", (cell_w * columns, top + cell_h * math.ceil(len(rows) / columns)), "white")
    draw = ImageDraw.Draw(image)
    font_path = Path("C:/Windows/Fonts/arial.ttf")
    font = ImageFont.truetype(str(font_path), 16) if font_path.exists() else ImageFont.load_default()
    heading = ImageFont.truetype(str(font_path), 26) if font_path.exists() else font
    draw.text((18, 14), title, fill="#132c3c", font=heading)
    draw.text((18, 52), subtitle, fill="#465a67", font=font)
    for position, row in enumerate(rows):
        left, upper = (position % columns) * cell_w, top + (position // columns) * cell_h
        draw.rectangle((left + 4, upper + 4, left + cell_w - 4, upper + cell_h - 4), outline="#d5dde2")
        record, metrics = row["record"], row["metrics"]
        draw.text((left + 12, upper + 10), f'{row["candidate_index"]:02d}  {row["category"]}', fill="#17394f", font=font)
        draw.text((left + 12, upper + cell_h - 46), record["key_id"], fill="#17394f", font=font)
        draw.text((left + 12, upper + cell_h - 24),
                  f'{metrics["stroke_count"]} strokes | {metrics["point_count"]} samples', fill="#657986", font=font)
        x0, y0, x1, y1 = metrics["coordinate_bounds"]
        extent = max(x1 - x0, y1 - y0, 1)
        scale = 165 / extent
        cx, cy = left + cell_w / 2, upper + 119
        for stroke in record["drawing"]:
            xy = [(cx + (x - (x0 + x1) / 2) * scale, cy + (y - (y0 + y1) / 2) * scale)
                  for x, y in zip(stroke[0], stroke[1])]
            if len(xy) == 1:
                x, y = xy[0]
                draw.ellipse((x - 1, y - 1, x + 1, y + 1), fill="#101820")
            else:
                draw.line(xy, fill="#101820", width=2, joint="curve")
    image.save(path)


def candidates(args: argparse.Namespace) -> None:
    args.output.mkdir(parents=True, exist_ok=True)
    source_files = list(args.source.glob("*.ndjson"))
    inventory = {
        "schema": "quickdraw-bounded-selection/1", "seed": args.seed,
        "source_directory": str(args.source.resolve()), "ndjson_file_count": len(source_files),
        "total_ndjson_bytes": sum(p.stat().st_size for p in source_files),
        "scope": "bounded prefix per category, not a corpus-representative sample",
        "filters": "only encoding-valid records; no recognized, geometry, country or time filter",
        "ordering": "ascending SHA-256 of seed:key_id, ties resolved by source line",
        "category_role": "sampling stratum/review label only; never geometric evidence",
        "sources": [],
    }
    for category in CATEGORIES:
        source = args.source / (category + ".ndjson")
        source_stat = source.stat()
        rows, rejected, recognized, strokes = [], [], Counter(), Counter()
        prefix_hash = hashlib.sha256()
        lines_read = bytes_read = 0
        with source.open("rb") as stream:
            for line_index in range(args.prefix):
                offset = stream.tell()
                raw = stream.readline()
                if not raw:
                    break
                prefix_hash.update(raw)
                lines_read += 1
                bytes_read += len(raw)
                try:
                    record = json.loads(raw)
                    if not isinstance(record, dict):
                        raise ValueError("record is not an object")
                    errors = validate_record(record)
                except (ValueError, TypeError) as error:
                    errors, record = [str(error)], {}
                if errors:
                    rejected.append({"source_line": line_index + 1, "byte_offset": offset, "errors": errors})
                    continue
                metrics = acquisition_metrics(record)
                recognized[str(record.get("recognized"))] += 1
                strokes[str(metrics["stroke_count"])] += 1
                rank = hashlib.sha256(f'{args.seed}:{record["key_id"]}'.encode()).hexdigest()
                rows.append({"category": category, "record": record, "raw": raw,
                             "source_line": line_index + 1, "byte_offset": offset, "byte_length": len(raw),
                             "record_sha256": hashlib.sha256(raw).hexdigest(),
                             "selection_hash": rank, "metrics": metrics})
        chosen = sorted(rows, key=lambda r: (r["selection_hash"], r["source_line"]))[:args.count]
        if len({row["record"]["key_id"] for row in chosen}) != len(chosen):
            raise ValueError(f"duplicate candidate key_id in {category}; inspect before selecting")
        for i, row in enumerate(chosen, 1):
            row["candidate_index"] = i
        category_path = args.output / category
        category_path.mkdir(exist_ok=True)
        (category_path / "candidates.ndjson").write_bytes(b"".join(row["raw"] for row in chosen))
        source_metadata = {
            "category": category, "path": str(source.resolve()), "file_size_bytes": source_stat.st_size,
            "file_mtime_ns": source_stat.st_mtime_ns, "prefix_records_read": lines_read,
            "prefix_bytes_read": bytes_read, "prefix_sha256": prefix_hash.hexdigest(),
            "encoding_valid_records": len(rows), "rejected": rejected,
            "recognized_metadata_counts": dict(recognized), "stroke_count_distribution": dict(strokes),
            "fractional_coordinate_records": sum(row["metrics"]["has_fractional_coordinates"] for row in rows),
            "consecutive_duplicate_xy_count": sum(row["metrics"]["consecutive_duplicate_xy_count"] for row in rows),
            "within_stroke_backwards_time_count": sum(row["metrics"]["within_stroke_backwards_time_count"] for row in rows),
            "candidate_count": len(chosen), "full_file_record_count": None,
        }
        manifest = [{key: value for key, value in row.items() if key not in ("raw", "record")}
                    | {"key_id": row["record"]["key_id"]} for row in chosen]
        write_json(category_path / "candidates.manifest.json", manifest)
        render_sheet(chosen, category_path / "candidates.png", f"Quick Draw | {category} | candidate review",
                     f'{len(chosen)} deterministic candidates from first {lines_read:,} records; original strokes, aspect preserved')
        inventory["sources"].append(source_metadata)
    write_json(args.output / "inventory.json", inventory)
    print(json.dumps({"output": str(args.output.resolve()), "sources": inventory["sources"]}, indent=2))


def select(args: argparse.Namespace) -> None:
    review = json.loads(args.review.read_text(encoding="utf-8"))
    combined, combined_raw, selection = [], [], []
    all_candidates = {}
    for category in CATEGORIES:
        folder = args.output / category
        manifest = json.loads((folder / "candidates.manifest.json").read_text(encoding="utf-8"))
        raw_lines = (folder / "candidates.ndjson").read_bytes().splitlines(keepends=True)
        by_id = {row["key_id"]: (row, raw) for row, raw in zip(manifest, raw_lines, strict=True)}
        all_candidates.update({(category, key): value for key, value in by_id.items()})
        chosen, chosen_raw = [], []
        seen = set()
        for choice in review["categories"][category]:
            key_id = choice["key_id"]
            if key_id in seen:
                raise ValueError(f"duplicate selected ID {key_id}")
            seen.add(key_id)
            item, raw = by_id[key_id]
            if hashlib.sha256(raw).hexdigest() != item["record_sha256"]:
                raise ValueError(f"candidate record hash mismatch: {key_id}")
            record = json.loads(raw)
            chosen.append(item | {"record": record, "review": choice})
            chosen_raw.append(raw)
            selection.append(item | {"review": choice})
        (folder / "selected.ndjson").write_bytes(b"".join(chosen_raw))
        render_sheet(chosen, folder / "selected.png", f"Quick Draw | {category} | proposed pilot selection",
                     "Visual curation for structural coverage; provisional until user review", columns=5)
        combined.extend(chosen)
        combined_raw.extend(chosen_raw)
    combined_output = args.combined_output or args.output
    combined_output.mkdir(parents=True, exist_ok=True)
    (combined_output / "selected.ndjson").write_bytes(b"".join(combined_raw))
    write_json(combined_output / "selection-manifest.json", {
        "schema": "quickdraw-visual-selection/1", "review": review,
        "source_inventory": str((args.output / "inventory.json").resolve()), "samples": selection,
    })
    render_sheet(combined, combined_output / "selected-overview.png", f"Quick Draw | {len(combined)} proposed pilot samples",
                 "Original source strokes; uniform aspect-preserving display; category is context, not structural truth", columns=10)
    challenge_rows, challenge_raw, challenge_manifest = [], [], []
    for choice in review.get("challenges", []):
        item, raw = all_candidates[(choice["category"], choice["key_id"])]
        if hashlib.sha256(raw).hexdigest() != item["record_sha256"]:
            raise ValueError(f'challenge record hash mismatch: {choice["key_id"]}')
        challenge_rows.append(item | {"record": json.loads(raw), "review": choice})
        challenge_raw.append(raw)
        challenge_manifest.append(item | {"review": choice})
    if challenge_rows:
        (combined_output / "challenges.ndjson").write_bytes(b"".join(challenge_raw))
        write_json(combined_output / "challenge-manifest.json", challenge_manifest)
        render_sheet(challenge_rows, combined_output / "challenges.png", "Additional acquisition challenges",
                     "Incomplete or ambiguous examples retained outside the initial controlled set", columns=5)
    write_review_page(combined_output, args.output, review, selection)
    print(json.dumps({"selected": len(combined), "output": str(args.output.resolve())}))


def write_review_page(destination: Path, selection_root: Path, review: dict, samples: list[dict]) -> None:
    """A local, dependency-free review artifact; this does not publish a site."""
    escape = html.escape
    sections = []
    for category in CATEGORIES:
        sheet = Path(os.path.relpath(selection_root / category / "selected.png", destination)).as_posix()
        candidate_sheet = Path(os.path.relpath(selection_root / category / "candidates.png", destination)).as_posix()
        rows = "".join(f'<tr><td>{item["candidate_index"]:02d}</td><td><code>{escape(item["key_id"])}</code></td>'
                       f'<td>{escape(item["review"]["visual_review_note"])}</td></tr>'
                       for item in samples if item["category"] == category)
        sections.append(f'<section id="{category}"><h2>{category.title()}</h2>'
                        f'<a href="{escape(sheet)}"><img src="{escape(sheet)}" alt="Selected {category} samples"></a>'
                        f'<details><summary>Selection notes and source IDs</summary><table><thead><tr><th>Candidate</th>'
                        f'<th>Source ID</th><th>Visual review note</th></tr></thead><tbody>{rows}</tbody></table></details>'
                        f'<p><a href="{escape(candidate_sheet)}">All 48 candidates</a></p></section>')
    challenge = ('<section><h2>Separate challenges</h2><p>Missing-support examples are retained outside the initial 50.</p>'
                 '<a href="challenges.png"><img src="challenges.png" alt="Additional challenge drawings"></a></section>'
                 if review.get("challenges") else "")
    document = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Quick Draw pilot review</title><style>
body{margin:0;background:#edf2f4;color:#193444;font:16px/1.55 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:32px}
h1{font-size:38px;line-height:1.2}h2{font-size:27px}section{padding:20px;background:white;border-radius:12px;margin:24px 0}
img{display:block;width:100%;height:auto}a{color:#125a86}nav a{margin-right:22px}table{border-collapse:collapse;width:100%}
th,td{text-align:left;vertical-align:top;border-bottom:1px solid #dce4e9;padding:9px}summary{cursor:pointer;padding:12px 0}
code{white-space:nowrap} .status{border-left:4px solid #d29422;padding-left:16px}
</style><main><h1>Quick Draw: first 50 samples</h1>'''
    document += f'<p class="status">{escape(review["status"])}. Original source strokes; display preserves aspect.</p>'
    document += f'<p>{escape(review["selection_goal"])}</p><p>{escape(review["selection_bias"])}</p>'
    document += '<nav>' + ''.join(f'<a href="#{c}">{c.title()}</a>' for c in CATEGORIES) + '</nav>'
    document += '<section><h2>All samples</h2><a href="selected-overview.png"><img src="selected-overview.png" alt="All 50 proposed samples"></a></section>'
    document += ''.join(sections) + challenge
    document += '<p><a href="selected.ndjson">Exact selected records</a> · <a href="selection-manifest.json">Provenance and curation manifest</a></p>'
    document += '<p>Source: Google Creative Lab, <a href="https://github.com/googlecreativelab/quickdraw-dataset">Quick, Draw! dataset</a>, '
    document += '<a href="https://creativecommons.org/licenses/by/4.0/">CC BY 4.0</a>. These sheets select and uniformly scale original strokes; notes are this experiment\'s annotations.</p></main></html>'
    (destination / "review.html").write_text(document, encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("candidates", "select"))
    parser.add_argument("--source", type=Path, default=Path("D:/_quickDraw/quickdraw"))
    parser.add_argument("--output", type=Path, default=Path("D:/_quickDraw/gojo-pilot/selection-v1"))
    parser.add_argument("--prefix", type=int, default=2000)
    parser.add_argument("--count", type=int, default=48)
    parser.add_argument("--seed", default="20261023")
    parser.add_argument("--review", type=Path)
    parser.add_argument("--combined-output", type=Path,
                        help="optional separate destination for the combined selection")
    args = parser.parse_args()
    if args.prefix <= 0 or args.count <= 0 or args.count > args.prefix:
        parser.error("require 0 < count <= prefix")
    if args.mode == "select" and args.review is None:
        parser.error("select requires --review")
    (candidates if args.mode == "candidates" else select)(args)


if __name__ == "__main__":
    main()
