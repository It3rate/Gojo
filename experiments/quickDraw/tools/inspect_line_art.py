"""Build an offline hover inspector from restored native scene exports.

The browser is a read surface. It never supplies annotations to the native
importer, joins coincident sites, or silently completes a missing boundary.
"""
from __future__ import annotations

import argparse
import gzip
import json
import re
from pathlib import Path


def load_scenes(paths):
    scenes = []
    for path in paths:
        files = sorted(path.glob("*.inspector.json")) if path.is_dir() else [path]
        if path.is_dir() and (path / "inspector.ndjson").exists():
            files.append(path / "inspector.ndjson")
        for file in files:
            contents = file.read_text(encoding="utf-8-sig")
            rows = contents.splitlines() if file.suffix == ".ndjson" else [contents]
            for row in rows:
                if not row.strip():
                    continue
                scene = json.loads(row)
                if scene.get("nativeRestoreVerified") is not True or "analysis" not in scene:
                    raise ValueError(f"Not a verified restored native scene: {file}")
                values = [value for edge in scene["analysis"]["edges"] for point in edge["controls"] for value in point]
                values += [value for site in scene["analysis"]["sites"] for value in [site["x"], site["y"]]]
                # Keep differences and two-term dot products exactly integral
                # in the browser's number type, not just individual positions.
                if any(not isinstance(value, int) or abs(value) > 2**24-1 for value in values):
                    raise ValueError("Inspector geometry exceeds its 24-bit coordinate capacity")
                if scene.get("archive"):
                    scene["archivePath"] = (file.parent / scene["archive"]).resolve().as_uri()
                scenes.append(scene)
    if not scenes:
        raise ValueError("No restored native inspector exports found")
    identifiers = [s["id"] for s in scenes]
    if len(identifiers) != len(set(identifiers)):
        raise ValueError("Duplicate scene identities")
    return scenes


def build(paths, output, references=None, public=False, default_scene=None):
    scenes = load_scenes(paths)
    sources = json.loads(references.read_text(encoding="utf-8-sig")) if references else None
    licences = []
    if sources:
        for source in sources["items"]:
            if source["role"] == "license":
                file = references.parent / source["collection"] / "LICENSE.txt"
                licences.append({"collection": source["collection"], "attribution": source["attribution"],
                                 "text": file.read_text(encoding="utf-8-sig")})
    if default_scene and default_scene not in {scene["id"] for scene in scenes}:
        raise ValueError("Default scene is absent from the restored collection")
    output.parent.mkdir(parents=True, exist_ok=True)
    selected_files = [output.name]
    if public:
        for scene in scenes:
            if not re.fullmatch(r"[a-zA-Z0-9_-]+", scene["id"]):
                raise ValueError("A public scene identity must be a safe filename")
            if Path(scene["archive"]).name != scene["archive"]:
                raise ValueError("A retained archive must be a sibling file")
            # Locate retained bytes through the verified input directory, not
            # through any externally supplied URI in the display envelope.
            candidates = [(path if path.is_dir() else path.parent) / scene["archive"] for path in paths]
            source = next((path for path in candidates if path.exists() and path.resolve().as_uri() == scene["archivePath"]), None)
            if source is None:
                raise ValueError("The restored scene's archive is unavailable")
            relative = f"archives/{scene['id']}.gojo.json.gz"
            target = output.parent / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(gzip.compress(source.read_bytes(), mtime=0))
            scene["archive"] = relative
            scene["archivePath"] = relative
            selected_files.append(relative)
        if sources:
            sources = {**sources, "items": [{key: value for key, value in item.items() if key != "file"}
                                          for item in sources["items"]]}
    data = json.dumps({"scenes": scenes, "references": sources, "licences": licences,
                       "public": public, "defaultScene": default_scene}, ensure_ascii=False)
    if public and ("file:///" in data or re.search(r"(?<![A-Za-z])[A-Za-z]:[\\/]", data)):
        raise ValueError("Public inspector contains a machine-local path")
    # A title or source label must never terminate the embedded JSON script.
    data = data.replace("<", "\\u003c").replace("\u2028", "\\u2028").replace("\u2029", "\\u2029")
    template = Path(__file__).with_name("line_art_inspector.html").read_text(encoding="utf-8")
    output.write_text(template.replace("__SCENE_DATA__", data), encoding="utf-8")
    if public:
        (output.parent / "public-files.json").write_text(json.dumps({"files": selected_files}, indent=2)+"\n", encoding="utf-8")
    return {"scenes": len(scenes), "edges": sum(len(s["analysis"]["edges"]) for s in scenes),
            "sites": sum(len(s["analysis"]["sites"]) for s in scenes),
            "regions": sum(len(s["analysis"].get("regions", [])) for s in scenes)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, nargs="+", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--references", type=Path)
    parser.add_argument("--public", action="store_true", help="Package portable gzip archives and omit machine-local paths")
    parser.add_argument("--default-scene")
    args = parser.parse_args()
    print(json.dumps(build(args.input, args.output, args.references, args.public, args.default_scene), indent=2))


if __name__ == "__main__":
    main()
