#!/usr/bin/env python3
"""Verify or restore manifest-listed research results using the standard library."""

import argparse
import gzip
import hashlib
import json
import os
from pathlib import Path, PurePosixPath, PureWindowsPath
import re
import sys
import tempfile
import zlib


CHUNK_BYTES = 1024 * 1024
REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
MANIFEST_PATH = REPOSITORY_ROOT / "experiments/ResearchArchive/large-results.json"


def guarded_path(root, relative):
    """Accept a portable repository-relative path and reject escaping symlinks."""
    if not isinstance(relative, str) or not relative or "\\" in relative:
        raise ValueError("Manifest paths must be nonempty relative paths with '/' separators.")
    parts = relative.split("/")
    if (PurePosixPath(relative).is_absolute() or PureWindowsPath(relative).drive
            or any(part in ("", ".", "..") or ":" in part or part.rstrip(" .") != part
                   or PureWindowsPath(part).is_reserved() for part in parts)):
        raise ValueError("Unsafe manifest path: " + relative)
    root = root.resolve()
    path = root.joinpath(*parts)
    if not path.resolve().is_relative_to(root):
        raise ValueError("Manifest path escapes its root: " + relative)
    return path


def load_entries():
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8-sig"))
    if not isinstance(manifest, dict) or not isinstance(manifest.get("files"), list) or not manifest["files"]:
        raise ValueError("The manifest must contain a nonempty 'files' array.")
    seen = set()
    for entry in manifest["files"]:
        if not isinstance(entry, dict):
            raise ValueError("Every manifest entry must be an object.")
        for name in ("path", "archive"):
            guarded_path(REPOSITORY_ROOT, entry.get(name))
        for name in ("bytes", "archiveBytes"):
            if type(entry.get(name)) is not int or entry[name] < 0:
                raise ValueError("Manifest size must be a nonnegative integer: " + name)
        for name in ("sha256", "archiveSha256"):
            if not isinstance(entry.get(name), str) or not re.fullmatch(r"[0-9a-fA-F]{64}", entry[name]):
                raise ValueError("Invalid manifest SHA-256: " + name)
        identity = os.path.normcase(str(guarded_path(REPOSITORY_ROOT, entry["path"])))
        if identity in seen:
            raise ValueError("Duplicate manifest target: " + entry["path"])
        seen.add(identity)
    return manifest["files"]


def digest_file(path):
    digest = hashlib.sha256()
    count = 0
    with path.open("rb") as stream:
        while block := stream.read(CHUNK_BYTES):
            digest.update(block)
            count += len(block)
    return count, digest.hexdigest()


def require_digest(actual, expected_size, expected_hash, label):
    if actual != (expected_size, expected_hash.lower()):
        raise ValueError("Size or SHA-256 mismatch: " + label)


def unpack_verified(archive, entry, output=None):
    digest = hashlib.sha256()
    count = 0
    with gzip.open(archive, "rb") as stream:
        while block := stream.read(CHUNK_BYTES):
            count += len(block)
            if count > entry["bytes"]:
                raise ValueError("Decompressed result exceeds its declared size: " + entry["path"])
            digest.update(block)
            if output is not None:
                output.write(block)
    require_digest((count, digest.hexdigest()), entry["bytes"], entry["sha256"], entry["path"])


def check_existing(target, entry):
    if not target.exists() and not target.is_symlink():
        return False
    if not target.is_file():
        raise ValueError("Existing target is not a regular file: " + entry["path"])
    require_digest(digest_file(target), entry["bytes"], entry["sha256"],
                   "existing file (refusing overwrite): " + entry["path"])
    return True


def restore(archive, entry, destination):
    target = guarded_path(destination, entry["path"])
    if check_existing(target, entry):
        unpack_verified(archive, entry)
        print("Already identical; kept: " + entry["path"])
        return
    target.parent.mkdir(parents=True, exist_ok=True)
    target = guarded_path(destination, entry["path"])
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(mode="wb", prefix="." + target.name + ".", suffix=".restore",
                                         dir=target.parent, delete=False) as stream:
            temporary = Path(stream.name)
            unpack_verified(archive, entry, stream)
            stream.flush()
            os.fsync(stream.fileno())
        # Same-directory atomic publication without overwriting a file that
        # appeared during verification. Unsupported filesystems fail safely.
        try:
            os.link(temporary, target)
        except FileExistsError:
            if not check_existing(guarded_path(destination, entry["path"]), entry):
                raise ValueError("Target changed during restore: " + entry["path"])
            print("Already identical; kept: " + entry["path"])
            return
        print("Restored: " + entry["path"])
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--verify", action="store_true", help="verify both archive and decompressed hashes (default)")
    mode.add_argument("--restore", action="store_true", help="restore missing originals; never replace a differing file")
    parser.add_argument("--destination", type=Path, default=REPOSITORY_ROOT,
                        help="root directory for restored relative paths (default: this checkout)")
    args = parser.parse_args(argv)
    try:
        entries = load_entries()
        for entry in entries:
            archive = guarded_path(REPOSITORY_ROOT, entry["archive"])
            require_digest(digest_file(archive), entry["archiveBytes"], entry["archiveSha256"], entry["archive"])
            if args.restore:
                restore(archive, entry, args.destination)
            else:
                unpack_verified(archive, entry)
                print("Verified archive and result: " + entry["path"])
        return 0
    except (OSError, EOFError, ValueError, KeyError, TypeError, zlib.error) as error:
        print("Archive error: " + str(error), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
