"""Render cited public-domain source excerpts, not generated ornament output.

Requires PyMuPDF and Pillow. Supply the PDF from the exact linked Archive scan;
the book itself is not part of the repository. Crop rectangles are manually
reviewed page fractions; they have no role in native generation or acceptance.

python experiments/QualitativeFrieze/research/render_bourgoin_sources.py \
    artifacts/bourgoin-research/source.pdf
"""
import argparse
import json
import re
from pathlib import Path

import fitz
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
DEFAULT = ROOT / "experiments/QualitativeFrieze/results/bourgoin-patterns"

# Fractions of source page width/height, including the figure caption where
# practical. These locate references, never measure or trace generated paths.
CROPS = {
    (274, 185): (.17, .30, .77, .47),
    (274, 186): (.16, .59, .78, .76),
    (275, 187): (.20, .28, .40, .43),
    (275, 188): (.44, .28, .63, .43),
    (275, 189): (.68, .28, .87, .43),
    (277, 196): (.26, .155, .79, .37),
    (277, 197): (.26, .408, .79, .61),
    (278, 201): (.20, .63, .74, .85),
    (279, 202): (.19, .14, .79, .38),
    (279, 203): (.19, .40, .79, .65),
    (392, 1): (.18, .175, .32, .212),
    (408, 4): (.19, .335, .38, .505),
    (408, 5): (.39, .335, .58, .505),
    (408, 10): (.19, .67, .39, .825),
    (408, 11): (.39, .67, .58, .825),
    (408, 12): (.59, .67, .80, .825),
    (418, 3): (.20, .335, .49, .505),
    (428, 4): (.20, .49, .49, .645),
}

# Later source studies keep reviewed crop locations beside their authoring
# helpers. These remain presentation references, never generation parameters.
for crop_file in Path(__file__).parent.glob("bourgoin_extended_*_crops.json"):
    for key, crop in json.loads(crop_file.read_text(encoding="utf-8")).items():
        CROPS[tuple(int(part) for part in key.split(":"))] = tuple(crop)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("pdf", type=Path)
    parser.add_argument("--output", type=Path, default=DEFAULT)
    args = parser.parse_args()
    catalogue = json.loads((args.output / "catalog.json").read_text(encoding="utf-8"))
    document = fitz.open(args.pdf)
    if len(document) != 444:
        raise ValueError("Expected the 444-leaf thoriedelorneme01bourgoog scan; verify the edition.")
    for entry in catalogue["Entries"]:
        leaf = entry["Leaf"]
        if not leaf:
            continue
        # Prefer an explicit figure number; plate numbers are not panel IDs.
        match = re.search(r"fig(?:ure)?s?\.?\s*(\d+)", entry["Reference"], re.I)
        figure = int(match.group(1)) if match else None
        crop = CROPS.get((leaf, figure), (.17, .16, .82, .83))
        page = document[leaf]
        rect = page.rect
        clip = fitz.Rect(crop[0] * rect.width, crop[1] * rect.height,
                         crop[2] * rect.width, crop[3] * rect.height)
        pix = page.get_pixmap(matrix=fitz.Matrix(2.5, 2.5), clip=clip, alpha=False)
        image = Image.frombytes("RGB", (pix.width, pix.height), pix.samples)
        image.thumbnail((1000, 900))
        image.save(args.output / f"source-{entry['Id']}.jpg", quality=90, optimize=True)
        print(f"{entry['Id']}: leaf {leaf}, figure {figure}, {image.width}x{image.height}")


if __name__ == "__main__":
    main()
