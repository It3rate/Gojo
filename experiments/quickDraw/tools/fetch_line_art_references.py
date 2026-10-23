"""Fetch three hash-pinned church references from their official repositories.

There is one church per collection, not a pack of distinct church drawings.
Original SVG and complete licence bytes are retained. Run this separately from
the bounded SVG importer; OpenMoji has filled artwork and is reference-only.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from urllib.request import Request, urlopen


SOURCES = [
    dict(collection="lucide", repo="lucide-icons/lucide",
         revision="7faf87628a1ad6175975bc595344337ce14a7db4", license="ISC",
         asset="icons/church.svg", licensePath="LICENSE",
         assetHash="7b27e22b59e5f4a088a9ce16a6451139a2df10fa39fc69334d0b29e916e6189d",
         licenseHash="b495047bd93a9b06913511076f504daba17d5bbeb3e0650f3bb53a4220329c57",
         attribution="Lucide Contributors", convertible=True),
    dict(collection="tabler", repo="tabler/tabler-icons",
         revision="a4ce1404bc6d24d3c365afe7b258d6bf6f48d62d", license="MIT",
         asset="icons/outline/building-church.svg", licensePath="LICENSE",
         assetHash="94a6e630b1c155dc7aed836a1e3fb985bdbb55c5abb0c871bea5223e5d2f4c1a",
         licenseHash="b740a1d46122672da62833e97f7e7c8a13fa85cbc7445b584b297cc00dde93db",
         attribution="Copyright (c) 2020-2026 Paweł Kuna", convertible=True),
    dict(collection="openmoji", repo="hfg-gmuend/openmoji",
         revision="aeb8bb3a59e2de39c754ac79180c8131c906acea", license="CC-BY-SA-4.0",
         asset="black/svg/26EA.svg", licensePath="LICENSE.txt",
         assetHash="a3adcd79e7f5591637f1338c810b59b8838892e07bd9346964405d8a091cb5ac",
         licenseHash="5e436ff8ffbb77d8607220e9bce20c8915d860010feeb6c1ebef5a85688e9b39",
         attribution="OpenMoji contributors, https://openmoji.org/", convertible=False),
]


def fetch(output: Path) -> dict:
    items = []
    for source in SOURCES:
        for role, key, hashkey, filename in [
            ("asset", "asset", "assetHash", "church.svg"),
            ("license", "licensePath", "licenseHash", "LICENSE.txt"),
        ]:
            url = f"https://raw.githubusercontent.com/{source['repo']}/{source['revision']}/{source[key]}"
            request = Request(url, headers={"User-Agent": "Gojo-line-art-references"})
            with urlopen(request, timeout=60) as response:
                data = response.read()
            digest = hashlib.sha256(data).hexdigest()
            if digest != source[hashkey]:
                raise ValueError(f"Hash mismatch for {source['collection']} {role}")
            target = output / source["collection"] / filename
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
            items.append(dict(collection=source["collection"], role=role,
                              file=str(target.resolve()), sourceUrl=url,
                              sourcePage=f"https://github.com/{source['repo']}/blob/{source['revision']}/{source[key]}",
                              revision=source["revision"], license=source["license"],
                              attribution=source["attribution"], sha256=digest,
                              bytes=len(data), centerlineImportSupported=source["convertible"]))
    manifest = dict(schema="gojo.line-art.external-references.v1", sourceCount=3,
                    scope="One church icon from each of three official collections; not a church-specific bulk pack.",
                    items=items, conversion="Lucide and Tabler use the bounded path-centerline importer; OpenMoji is original reference-only because its filled compound outline is unsupported.")
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return manifest


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=Path("D:/_quickDraw/gojo-line-art/references"))
    args = parser.parse_args()
    result = fetch(args.output)
    print(json.dumps({"collections": result["sourceCount"], "files": len(result["items"]), "output": str(args.output)}))
