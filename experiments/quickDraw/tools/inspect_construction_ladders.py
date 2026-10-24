"""Package a read-only construction viewer from verified native replay exports.

Native archives own the program, executed structure and retained witnesses.
This adapter groups display variants and deduplicates repeated observations of
the same exported identities. It does not execute recipes or infer joins,
dependencies, containment, or construction order from coordinates.
"""
from __future__ import annotations

import argparse
import base64
import gzip
import json
import re
from collections import OrderedDict
from pathlib import Path


SAFE_ID = re.compile(r"[A-Za-z0-9_-]+\Z")


def load_exports(paths):
    runs = []
    for path in paths:
        files = [path / "inspector.ndjson"] if path.is_dir() else [path]
        for file in files:
            contents = file.read_text(encoding="utf-8-sig")
            rows = contents.splitlines() if file.suffix == ".ndjson" else [contents]
            for row in rows:
                if not row.strip():
                    continue
                run = json.loads(row)
                if run.get("nativeRestoreVerified") is not True or run.get("nativeReplayVerified") is not True:
                    raise ValueError("Construction export must be restored and replayed natively")
                if not SAFE_ID.fullmatch(run["id"]) or not SAFE_ID.fullmatch(run["family"]):
                    raise ValueError("Construction and family identities must be safe filenames")
                filename = run["archive"]
                if Path(filename).name != filename or "/" in filename or "\\" in filename:
                    raise ValueError("The native archive must be a sibling of its verified export")
                archive = file.parent / filename
                if not archive.is_file():
                    raise ValueError("The retained native archive is unavailable")
                runs.append((run, archive))
    if not runs:
        raise ValueError("No verified construction exports found")
    if len({run["id"] for run, _ in runs}) != len(runs):
        raise ValueError("Duplicate construction run identity")
    return runs


def normalize(run):
    """Project repeated native observations to one pool and explicit memberships."""
    edges, sites, rungs = OrderedDict(), OrderedDict(), []
    earlier_rung_ids = set()
    for rung in run["rungs"]:
        if rung.get("verifiedNativeRestore") is not True or rung.get("verifiedNativeReplay") is not True:
            raise ValueError("Every published rung needs native restore and replay verification")
        for site in rung["sites"]:
            numeric = [site["x"], site["y"]]
            if any(type(value) is not int or abs(value) > 2**24 - 1 for value in numeric):
                raise ValueError("Display coordinates exceed their selected integer capacity")
            if site["id"] in sites and sites[site["id"]] != site:
                raise ValueError("One site identity changed across captured rung observations")
            sites[site["id"]] = site
        visible, guides, introduced, refs = [], [], [], OrderedDict()
        for edge in rung["edges"]:
            stable = {key: edge[key] for key in ("id", "label", "at", "controls", "controlSiteIds", "from", "to")}
            if edge["id"] in edges and edges[edge["id"]] != stable:
                raise ValueError("One edge identity changed across captured rung observations")
            if not 2 <= len(edge["controls"]) <= 4:
                raise ValueError("Only the exported finite line / Bézier read is supported")
            if any(type(value) is not int or abs(value) > 2**24 - 1
                   for point in edge["controls"] for value in point):
                raise ValueError("Display coordinates exceed their selected integer capacity")
            if any(site not in sites for site in edge["controlSiteIds"]):
                raise ValueError("An edge references a site absent from its captured rung")
            edges[edge["id"]] = stable
            (guides if edge["guide"] else visible).append(edge["id"])
            if edge["introduced"]:
                introduced.append(edge["id"])
                # This grouping is a display projection of consumed native site
                # references. Coordinate equality creates no additional entries.
                for dep in edge["dependencies"]:
                    if dep["rungId"] not in earlier_rung_ids | {rung["id"]}:
                        raise ValueError("A dependency refers to an unpublished later rung")
                    if dep["siteId"] not in sites:
                        raise ValueError("A dependency refers to an unavailable native site")
                    key = (edge["id"], dep["rungId"], dep["sameRung"])
                    ref = refs.setdefault(key, {
                        "targetEdgeIds": [edge["id"]], "sourceRungId": dep["rungId"],
                        "sameRung": dep["sameRung"], "sourceEdgeIds": [], "sourceSiteIds": [],
                        "label": "Consumes earlier sites within this step" if dep["sameRung"] else "Consumes retained construction sites",
                    })
                    if dep["siteId"] not in ref["sourceSiteIds"]:
                        ref["sourceSiteIds"].append(dep["siteId"])
                    if dep["edgeId"] and dep["edgeId"] not in ref["sourceEdgeIds"]:
                        ref["sourceEdgeIds"].append(dep["edgeId"])
        rungs.append({"id": rung["id"], "title": rung["title"], "note": rung["note"],
                      "visibleEdgeIds": visible, "guideEdgeIds": guides,
                      "introducedEdgeIds": introduced, "dependencies": list(refs.values()),
                      "constraints": rung["constraints"]})
        earlier_rung_ids.add(rung["id"])
    return {"id": run["id"], "label": run["variantLabel"], "parameters": run["parameters"],
            "viewBox": run["viewBox"], "analysis": {"edges": list(edges.values()), "sites": list(sites.values())},
            "rungs": rungs, "nativeRestoreVerified": True, "nativeReplayVerified": True,
            "verificationNote": "The saved native program was restored and executed again. Geometry and condition reads were checked against the first execution. " + run.get("numericPolicy", "")}


def build(paths, output, public=False, default_scene=None, public_archives="all"):
    families, archive_copies = OrderedDict(), []
    for run, archive in load_exports(paths):
        variant = normalize(run)
        program_name = run.get("programArchive")
        if program_name:
            if Path(program_name).name != program_name or "/" in program_name or "\\" in program_name:
                raise ValueError("The native program archive must be a sibling file")
            archive = archive.parent / program_name
            if not archive.is_file():
                raise ValueError("The retained executable program archive is unavailable")
        if public:
            selected = public_archives == "all" or (public_archives == "representative" and run["family"] not in families)
            if selected:
                relative = f"archives/{run['id']}.gojo.json.gz"
                variant["archivePath"] = relative
                variant["archiveKind"] = "program" if program_name else "execution"
                archive_copies.append((archive, relative))
        else:
            variant["archivePath"] = archive.resolve().as_uri()
            variant["archiveKind"] = "program" if program_name else "execution"
        family = families.setdefault(run["family"], {"id": run["family"], "title": run["title"],
                                                    "description": run["description"], "viewBox": run["viewBox"], "variants": []})
        family["variants"].append(variant)
    if default_scene and default_scene not in families:
        raise ValueError("Default construction family is absent")
    data = {"schema": "gojo.construction-viewer.v1", "public": public,
            "defaultScene": default_scene or next(iter(families)), "scenes": list(families.values())}
    encoded = json.dumps(data, ensure_ascii=False, separators=(",", ":"))
    if public and ("file:///" in encoded or re.search(r"(?<![A-Za-z])[A-Za-z]:[\\/]", encoded)):
        raise ValueError("Public construction viewer contains a machine-local path")
    if public:
        # One embedded compressed read also works from a local file without a
        # server or cross-origin fetch. Compression changes no read semantics.
        payload = base64.b64encode(gzip.compress(encoded.encode("utf-8"), mtime=0)).decode("ascii")
        encoded = json.dumps({"encoding": "gzip-base64", "payload": payload}, separators=(",", ":"))
    encoded = encoded.replace("<", "\\u003c").replace("\u2028", "\\u2028").replace("\u2029", "\\u2029")
    output.parent.mkdir(parents=True, exist_ok=True)
    template = Path(__file__).with_name("construction_ladder_viewer.html").read_text(encoding="utf-8")
    output.write_text(template.replace("__CONSTRUCTION_DATA__", encoded), encoding="utf-8")
    files = [output.name]
    for archive, relative in archive_copies:
        target = output.parent / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(gzip.compress(archive.read_bytes(), mtime=0))
        files.append(relative)
    if public:
        (output.parent / "public-files.json").write_text(json.dumps({"files": files}, indent=2)+"\n", encoding="utf-8")
    return {"families": len(families), "variants": sum(len(s["variants"]) for s in families.values()),
            "rungs": sum(len(v["rungs"]) for s in families.values() for v in s["variants"]),
            "publicFiles": len(files) if public else 0, "htmlBytes": output.stat().st_size}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, nargs="+", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--public", "--portable", dest="public", action="store_true",
                        help="Prepare a portable folder with embedded reads and compressed programs; does not deploy or publish.")
    parser.add_argument("--default-scene")
    parser.add_argument("--public-archives", choices=("all", "representative", "none"), default="all")
    args = parser.parse_args()
    print(json.dumps(build(args.input, args.output, args.public, args.default_scene, args.public_archives), indent=2))


if __name__ == "__main__":
    main()
