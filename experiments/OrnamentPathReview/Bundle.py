"""Bundle the local path study. Keep all paid search ledgers in the full archive."""
import hashlib
import json
import sys
import tempfile
from pathlib import Path
from zipfile import ZipFile, ZipInfo, ZIP_DEFLATED


def bundle(root,destination,compact):
    files=[p for p in root.rglob("*") if p.is_file() and p.suffix not in (".zip",".tmp") and not p.name.startswith(".")]
    with tempfile.NamedTemporaryFile(dir=destination.parent,suffix=".tmp",delete=False) as handle:
        temp=Path(handle.name)
    try:
        checks={}
        with ZipFile(temp,"w") as archive:
            for p in sorted(files,key=lambda path:path.relative_to(root).as_posix()):
                key=p.relative_to(root).as_posix()
                if compact and (key.startswith("runtime-") or "histories" in p.relative_to(root).parts
                                or p.suffix in (".pdb",".exe",".dll",".log")):
                    continue
                data=p.read_bytes()
                checks[key]=hashlib.sha256(data).hexdigest()
                archive.writestr(ZipInfo(key),data,ZIP_DEFLATED,6)
            archive.writestr(ZipInfo("checksums.json"),json.dumps(checks,indent=2),ZIP_DEFLATED,6)
        with ZipFile(temp) as archive:
            if archive.testzip() is not None:
                raise ValueError("Archive failed verification")
        temp.replace(destination)
        print(f"{destination}: {len(checks)} files, {destination.stat().st_size:,} bytes")
    finally:
        temp.unlink(missing_ok=True)


if __name__=="__main__":
    root=Path(sys.argv[1]).resolve()
    target=Path(sys.argv[2]).resolve()
    bundle(root,target,True)
    bundle(root,root/"CompleteRun.zip",False)
