"""Prepare a visually reviewed, evaluation-only transfer batch.

Categories stratify acquisition and review; they are not fitting or generation
targets. Source bytes, timestamps, and stroke/sample order are never rewritten.
The drawings-only export has no category, recognition status, source ID or time
metadata outside the actual drawing arrays. All outputs stay outside the repo.
"""
from __future__ import annotations

import argparse
import hashlib
import html
import json
from pathlib import Path

import select_samples


CATEGORIES = ("house", "star", "fish", "envelope", "umbrella")
# Fixed after visual inspection of deterministic candidate sheets, independent
# of fit quality. These are transfer fixtures, not representative samples.
REVIEW = {
    "house": [
        (7, "4658428846800896", "Plain roof and body; three strokes, slight roof/body gap and near-horizontal edge."),
        (8, "5029217295138816", "One-stroke roof and body with bowed edges; retained retracing and construction order."),
        (20, "5868815621554176", "Plain roof/body with a visible peak gap; do not silently close it."),
        (26, "6123086950694912", "Roof/body plus one small window; separates enclosing support from a later detail."),
        (30, "4572978828279808", "Wide roof/body with a door; open joins and skew remain part of the drawing."),
    ],
    "star": [
        (8, "5152841154953216", "Wide crossing pentagram route; one stroke with modest asymmetry."),
        (15, "6116169515794432", "Single outline with rounded bends; no internal crossing implied by category."),
        (19, "5378647596728320", "Tall crossing route with a short open tail; acquired overlap is not a welded junction."),
        (23, "4683734626336768", "Outline with a gap near the upper-left valley; preserves missing support."),
        (28, "6334666367827968", "Bowed asymmetric outline with a visible break; a different topology from pentagrams."),
    ],
    "fish": [
        (1, "6162644367048704", "One-stroke rounded body and crossed tail, facing right; no eye or extra decoration."),
        (12, "5637798658834432", "Long narrow body, left tail and small eye; includes modest detail without clutter."),
        (14, "5891285699264512", "Compact body with right tail, facing left; opposite orientation retained."),
        (16, "5187178223108096", "Angular tail joined to curved body and small mouth; one acquired stroke."),
        (28, "5016110665564160", "Two-stroke body/tail construction; apparent contact does not merge acquisition identities."),
    ],
    "envelope": [
        (7, "5663065016107008", "Skewed outer boundary and flap with gaps/overshoot; three strokes."),
        (9, "6205068275613696", "One-stroke box and diagonal flap route; narrow tall aspect and corner overshoot."),
        (15, "6086480957014016", "Sparse one-stroke open border and shallow flap; gaps are visible source evidence."),
        (23, "6668369770053632", "Very wide shallow border and low flap; preserves aspect rather than template proportions."),
        (29, "6121321849159680", "Bowed border and curved asymmetric flap; one stroke and nonideal edges retained."),
    ],
    "umbrella": [
        (1, "5867500203933696", "Scalloped canopy and separate large curved handle; uneven lobes and contact gap."),
        (10, "6295189305950208", "Simple asymmetric dome, near-horizontal base and straight stem; no hooked-handle template."),
        (14, "4622689484406784", "Two-stroke domed canopy and hooked handle; tilted baseline and bowed stem."),
        (17, "6004716511166464", "Slanted canopy/stem with one short interior line; orientation and isolated detail retained."),
        (19, "5468440313724928", "Wide scalloped canopy and curved handle; gap at left edge and unequal repeated curves."),
    ],
}


def candidates(args):
    # Reuse the exact acquisition selector, with a distinct review stratum set.
    select_samples.CATEGORIES = CATEGORIES
    select_samples.candidates(args)


def select(args):
    chosen = []
    source_by_category = {
        row["category"]: row for row in json.loads(
            (args.output / "inventory.json").read_text(encoding="utf-8"))["sources"]
    }
    for category in CATEGORIES:
        folder = args.output / category
        manifest = json.loads((folder / "candidates.manifest.json").read_text(encoding="utf-8"))
        raw_lines = (folder / "candidates.ndjson").read_bytes().splitlines(keepends=True)
        rows = {row["candidate_index"]: (row, raw)
                for row, raw in zip(manifest, raw_lines, strict=True)}
        for candidate_index, expected_id, note in REVIEW[category]:
            row, raw = rows[candidate_index]
            if row["key_id"] != expected_id:
                raise ValueError("Candidate order differs from visual review; restore default seed and prefix")
            if hashlib.sha256(raw).hexdigest() != row["record_sha256"]:
                raise ValueError("Candidate bytes changed; regenerate and re-review")
            source_path = Path(source_by_category[category]["path"])
            with source_path.open("rb") as stream:
                stream.seek(row["byte_offset"])
                if stream.read(row["byte_length"]) != raw:
                    raise ValueError("Source bytes no longer match retained candidate")
            chosen.append(row | {"record": json.loads(raw), "raw": raw,
                                 "visual_review_note": note, "source_path": str(source_path)})
    destination = args.output.parent
    destination.mkdir(parents=True, exist_ok=True)
    source_bytes = b"".join(row["raw"] for row in chosen)
    (destination / "selected.ndjson").write_bytes(source_bytes)
    # Record index maps back through the audit manifest; it is not an input
    # feature. Whitelist only drawing, retaining the real x/y/t arrays.
    with (destination / "drawings-only.ndjson").open("w", encoding="utf-8", newline="\n") as stream:
        for row in chosen:
            stream.write(json.dumps({"drawing": row["record"]["drawing"]}, separators=(",", ":")) + "\n")
    manifest = {
        "schema": "quickdraw-deferred-transfer-selection/1",
        "status": "visually reviewed evaluation fixture; deferred until core reconstruction works",
        "scope": "five additional categories, five simple instances each; curated, not representative",
        "category_role": "audit-only sampling stratum, never a learner feature or ideal target",
        "selection_policy": "encoding-valid deterministic candidates, visual review before any fitting",
        "selected_ndjson_sha256": hashlib.sha256(source_bytes).hexdigest(),
        "drawings_only_sha256": hashlib.sha256((destination / "drawings-only.ndjson").read_bytes()).hexdigest(),
        "source_inventory": str((args.output / "inventory.json").resolve()),
        "chronology": "exact source stroke/sample order and time arrays retained; never sort by timestamp",
        "samples": [{"record_index": index, **{key: value for key, value in row.items()
                    if key not in ("record", "raw")}}
                    for index, row in enumerate(chosen)],
    }
    select_samples.write_json(destination / "selection-manifest.json", manifest)
    select_samples.render_sheet(chosen, destination / "selected-overview.png",
        "Quick Draw | deferred transfer review | 25 original drawings",
        "Five acquisition strata; original orientation and aspect; labels are audit context only", columns=5)
    render_order_sheet(chosen, destination / "draw-order.png")
    sections = []
    for category in CATEGORIES:
        rows = "".join(f'<tr><td>{row["candidate_index"]:02d}</td><td>{row["key_id"]}</td>'
                       f'<td>{html.escape(row["visual_review_note"])}</td></tr>'
                       for row in chosen if row["category"] == category)
        sections.append(f'<h2>{category.title()}</h2><table>{rows}</table>')
    (destination / "review.html").write_text(
        '<!doctype html><meta charset="utf-8"><title>Quick Draw deferred transfer</title>'
        '<style>body{font:16px/1.5 system-ui;margin:32px;color:#213747}img{max-width:100%}'
        'td{padding:8px;border-bottom:1px solid #ddd}table{border-collapse:collapse}</style>'
        '<h1>Deferred transfer: 25 original drawings</h1>'
        '<p>Visual curation completed before any fitting. This is a small development transfer '
        'fixture, not a representative population or an untouched statistical benchmark. '
        'Category names are acquisition strata; no ideal target is inferred from them.</p>'
        '<img src="selected-overview.png" alt="25 original drawings">'
        '<h2>Draw order</h2><p>Colors cycle by recorded stroke; numbered dots mark stroke starts. '
        'The recorded stroke and sample order is preserved even if timestamps disagree.</p>'
        '<img src="draw-order.png" alt="Original drawings colored by stroke order">'
        + ''.join(sections) +
        '<p><a href="selected.ndjson">Unchanged source bytes</a> | '
        '<a href="drawings-only.ndjson">Drawing-only input</a> | '
        '<a href="selection-manifest.json">Source hashes and visual review</a></p>'
        '<p>Source: Google Creative Lab, '
        '<a href="https://github.com/googlecreativelab/quickdraw-dataset">Quick, Draw! dataset</a>, '
        '<a href="https://creativecommons.org/licenses/by/4.0/">CC BY 4.0</a>. '
        'Curation and order overlays are derived work.</p>', encoding="utf-8")
    print(json.dumps({"output": str(destination), "selected": len(chosen),
                      "strokes": sum(row["metrics"]["stroke_count"] for row in chosen),
                      "points": sum(row["metrics"]["point_count"] for row in chosen),
                      "sha256": manifest["selected_ndjson_sha256"]}))


def render_order_sheet(rows, path):
    from PIL import Image, ImageDraw, ImageFont
    width, height, top, cols = 285, 270, 95, 5
    image = Image.new("RGB", (width * cols, top + height * 5), "white")
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 16)
    heading = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 25)
    colors = ("#1565c0", "#d84315", "#2e7d32", "#8e24aa", "#00838f", "#b8860b", "#c62828")
    draw.text((18, 14), "Quick Draw | recorded stroke order", fill="#132c3c", font=heading)
    draw.text((18, 54), "Numbered dots are stroke starts; colors repeat after seven strokes; no joining or repair", fill="#465a67", font=font)
    for position, row in enumerate(rows):
        left, upper = (position % cols) * width, top + (position // cols) * height
        draw.rectangle((left+4, upper+4, left+width-4, upper+height-4), outline="#d5dde2")
        draw.text((left+12, upper+10), f'{row["candidate_index"]:02d} {row["category"]}', fill="#17394f", font=font)
        draw.text((left+12, upper+height-30), row["key_id"], fill="#17394f", font=font)
        x0, y0, x1, y1 = row["metrics"]["coordinate_bounds"]
        scale = 165/max(x1-x0, y1-y0, 1)
        for index, stroke in enumerate(row["record"]["drawing"]):
            points = [(left+width/2+(x-(x0+x1)/2)*scale, upper+124+(y-(y0+y1)/2)*scale)
                      for x, y in zip(stroke[0], stroke[1])]
            color = colors[index % len(colors)]
            if len(points) > 1:
                draw.line(points, fill=color, width=2, joint="curve")
            if points:
                x, y = points[0]
                draw.ellipse((x-3, y-3, x+3, y+3), fill=color)
                draw.text((x+4, y-15), str(index+1), fill=color, font=font)
    image.save(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("candidates", "select"))
    parser.add_argument("--source", type=Path, default=Path("D:/_quickDraw/quickdraw"))
    parser.add_argument("--output", type=Path,
                        default=Path("D:/_quickDraw/gojo-reconstruction/transfer/candidates"))
    parser.add_argument("--prefix", type=int, default=2000)
    parser.add_argument("--count", type=int, default=30)
    parser.add_argument("--seed", default="20261023-transfer")
    args = parser.parse_args()
    if args.prefix <= 0 or not 0 < args.count <= args.prefix:
        parser.error("require 0 < count <= prefix")
    (candidates if args.mode == "candidates" else select)(args)


if __name__ == "__main__":
    main()
