"""Package one completed local campaign. Neither archive runs an experiment."""
import argparse
import hashlib
import json
import tempfile
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

from Audit import audit


def include(relative, compact):
    if relative.name.startswith("."):
        return False
    if not compact:
        return True
    top = relative.parts[0]
    if top.startswith("runtime-") or top == "proposal-transfer":
        return False
    if relative.suffix == ".gz" or (relative.suffix == ".log" and relative.name != "timing.log"):
        return False
    if "islands" in relative.parts:
        return relative.name in ("winner.json", "winner.genome")
    return True


def bundle(root, output, compact):
    sources = ["primary", "proposal-transfer", "proposal-transfer-v2", "seam-location",
               "mirror-followup", "owner-exchange", "verification"]
    sources.extend(p.name for p in root.glob("runtime-*") if p.is_dir())
    files = [root / "reserve-gate.json"]
    for source in sources:
        files.extend(p for p in (root / source).rglob("*") if p.is_file())
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=output.parent, delete=False, suffix=".tmp") as tmp:
        temporary = Path(tmp.name)
    try:
        hashes = {}
        with ZipFile(temporary, "w", ZIP_DEFLATED, compresslevel=6) as archive:
            for path in sorted(files, key=lambda p: p.relative_to(root).as_posix()):
                relative = path.relative_to(root)
                if include(relative, compact):
                    contents = path.read_bytes()
                    key = relative.as_posix()
                    hashes[key] = hashlib.sha256(contents).hexdigest()
                    archive.writestr(ZipInfo(key), contents, ZIP_DEFLATED, 6)
            archive.writestr(ZipInfo("checksums.json"), json.dumps(hashes, indent=2), ZIP_DEFLATED, 6)
        with ZipFile(temporary) as archive:
            if archive.testzip() is not None:
                raise ValueError("Archive verification failed")
        temporary.replace(output)
        print(f"{output}: {len(hashes)} records, {output.stat().st_size:,} bytes")
    finally:
        temporary.unlink(missing_ok=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("run", type=Path)
    parser.add_argument("compact", type=Path)
    args = parser.parse_args()
    audit(args.run)
    bundle(args.run, args.compact, True)
    bundle(args.run, args.run / "CompleteRun.zip", False)
