"""Publish the recorded glyph atlas; do not rerun or reinterpret the experiment."""

import argparse
import hashlib
import json
from pathlib import Path

import markdown

WEB = Path(__file__).resolve().parents[1]
REPO = WEB.parents[1]
SOURCE = REPO / "experiments/QualitativeFrieze/results/glyph-joints"
DEST = WEB / "ornaments/glyph-joints"
ROUTE = "/ornaments/glyph-joints/"
RECORDS = ("README.md", "results.json", "corpus.gojo", "construction.gojo",
           "usage.gojo", "contexts.gojo")


def overview(report):
    """Display captured L/F/H strokes, with their recorded traversal directions."""
    parts = ['<svg xmlns="http://www.w3.org/2000/svg" width="900" height="300" '
             'viewBox="0 0 900 300" role="img" aria-labelledby="title desc">',
             '<title id="title">Glyph joint atlas</title>',
             '<desc id="desc">Recorded L, F and H constructions: a continuous bend, '
             'later attachments, and a bridge between two stems. Arrows follow pen strokes.</desc>',
             '<rect width="900" height="300" fill="#f5f4ef"/>']
    colors = ("#157e79", "#b43e64", "#4666b5")
    for i, (glyph, caption) in enumerate((("L", "Continuous bend"),
                                         ("F", "Later attachments"),
                                         ("H", "Connecting stems"))):
        f = next(x for x in report["Corpus"]
                 if x["Glyph"] == glyph and x["VariantId"] == "baseline")
        lines = {line["Index"]: line for line in f["Lines"]}
        parts.append(f'<svg x="{i * 300 + 60}" y="20" width="190" height="215" '
                     'viewBox="-2 -12 13 14">')
        for stroke in f["Strokes"]:
            color = colors[stroke["Index"] % len(colors)]
            marker = f'arrow-{i}-{stroke["Index"]}'
            parts.append(f'<defs><marker id="{marker}" viewBox="0 0 10 10" '
                         'refX="9" refY="5" markerWidth="4" markerHeight="4" '
                         f'orient="auto-start-reverse"><path d="M0 0L10 5L0 10Z" fill="{color}"/>'
                         '</marker></defs>')
            for visit in stroke["Visits"]:
                line = lines[visit["Section"]]
                assert line["Straight"], "The preview only displays these recorded straight strokes"
                points = line["Controls"][::-1] if visit["Reverse"] else line["Controls"]
                coords = " L".join(f'{p["X"]},{-p["Y"]}' for p in points)
                parts.append(f'<path d="M{coords}" fill="none" stroke="{color}" '
                             f'stroke-width=".25" marker-end="url(#{marker})"/>')
        for site in f["Sites"]:
            parts.append(f'<circle cx="{site["X"]}" cy="{-site["Y"]}" r=".14" '
                         'fill="#fffefa" stroke="#202d35" stroke-width=".08"/>')
        parts.append('</svg>')
        parts.append(f'<text x="{i * 300 + 150}" y="270" text-anchor="middle" '
                     f'font-family="system-ui,sans-serif" font-size="20" fill="#202d35">'
                     f'{glyph} · {caption}</text>')
    parts.append('</svg>')
    return "\n".join(parts) + "\n"


def publication():
    # Native archives are opaque bytes. Normalize only the text records, so
    # their publication and fingerprints are reproducible across checkouts.
    files = {name: ((SOURCE / name).read_bytes() if name.endswith('.gojo') else
                    (SOURCE / name).read_text(encoding="utf-8-sig").encode("utf-8"))
             for name in RECORDS}
    atlas = (SOURCE / "index.html").read_text(encoding="utf-8-sig")
    atlas = atlas.replace('<head>', f'<head><base href="{ROUTE}">', 1)
    atlas = atlas.replace('<body>', '<body><nav class="wrap" aria-label="Gojo navigation" '
                          'style="padding-top:18px;padding-bottom:18px;display:flex;gap:24px;flex-wrap:wrap">'
                          '<a href="/">Gojo home</a><a href="/ornaments/">Ornament studies</a></nav>', 1)
    old = '<a href="README.md">Markdown record</a>'
    assert old in atlas, "The source navigation changed; review the publication adapter"
    atlas = atlas.replace(old, '<a href="notes.html" target="_blank" rel="noopener">Read the notes</a>', 1)
    files["index.html"] = atlas.encode("utf-8")
    body = markdown.markdown(files["README.md"].decode("utf-8-sig"),
                             extensions=["tables", "fenced_code", "toc", "sane_lists"])
    # Long technical tables scroll within their own region on narrow screens.
    body = body.replace('<table>', '<div class="table-scroll" tabindex="0" role="region" '
                        'aria-label="Scrollable data table"><table>')
    body = body.replace('</table>', '</table></div>')
    notes = '''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<base href="/ornaments/glyph-joints/"><title>Glyph joint atlas — Notes · Gojo</title>
<style>
:root{color-scheme:light}*{box-sizing:border-box}body{margin:0;background:#f5f4ef;color:#202d35;font:17px/1.65 system-ui,sans-serif}
main{max-width:1080px;margin:auto;padding:30px clamp(18px,4vw,48px) 80px}nav{display:flex;gap:12px 24px;flex-wrap:wrap;padding-bottom:28px;border-bottom:1px solid #d9dfdc}
a{color:#126c68;text-underline-offset:3px}h1{font-size:clamp(30px,5vw,48px);line-height:1.15}h2{margin-top:48px;line-height:1.25}h3{margin-top:32px}
p,li{max-width:78ch}pre,.table-scroll{max-width:100%;overflow:auto}pre{background:#fffefa;padding:18px;border:1px solid #d9dfdc}code{font-size:.9em;overflow-wrap:anywhere}
table{border-collapse:collapse;background:#fffefa;width:100%;font-size:14px}th,td{border:1px solid #d9dfdc;padding:10px 12px;text-align:left;vertical-align:top;min-width:130px}
th{background:#e7eeea}.table-scroll:focus-visible,a:focus-visible{outline:3px solid #157e79;outline-offset:3px}
</style></head><body><main><nav aria-label="Study navigation"><a href="/">Gojo home</a><a href="/ornaments/">Ornament studies</a><a href="index.html">Interactive atlas</a><a href="README.md" download>Download Markdown</a></nav>
'''+body+'\n</main></body></html>\n'
    files["notes.html"] = notes.encode("utf-8")
    report = json.loads(files["results.json"])
    files["GlyphJointOverview.svg"] = overview(report).encode("utf-8")
    manifest = {
        "source": SOURCE.relative_to(REPO).as_posix(),
        "route": ROUTE,
        "scope": "All recorded glyph variants, route machines, contextual reads and native archives. Presentation-only adaptation.",
        "glyphs": len({f["Glyph"] for f in report["Corpus"]}),
        "variants": len(report["Corpus"]),
        "files": [{"file": name, "sha256": hashlib.sha256(data).hexdigest(),
                   "bytes": len(data)} for name, data in files.items()],
    }
    files["publication.json"] = (json.dumps(manifest, indent=2) + "\n").encode("utf-8")
    return files


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Check the committed publication without writing")
    args = parser.parse_args()
    files = publication()
    if args.check:
        stale = [name for name, data in files.items()
                 if not (DEST / name).is_file() or (DEST / name).read_bytes() != data]
        if stale:
            raise SystemExit("Rebuild the glyph atlas: " + ", ".join(stale))
    else:
        DEST.mkdir(parents=True, exist_ok=True)
        for name, data in files.items():
            (DEST / name).write_bytes(data)
    print(f'{"Checked" if args.check else "Published"} {len(files)} files, '
          f'{sum(map(len, files.values())):,} bytes, at {ROUTE}')


if __name__ == "__main__":
    main()
