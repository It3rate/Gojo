"""Check the saved seals and measure how the infill winners changed occupancy."""
import csv
import hashlib
import json
import sys
from pathlib import Path


def recorded(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def check_hash(path, expected):
    actual = hashlib.sha256(path.read_bytes()).hexdigest().upper()
    if actual != expected:
        raise ValueError(f"{path}: expected {expected}, found {actual}")


def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as source:
        yield from csv.DictReader(source)


def audit(folder):
    primary = folder / "primary"
    gate = recorded(folder / "reserve-gate.json")
    gate_files = {
        "SourceReport": "primary/Report.md",
        "PrimaryWinners": "primary/frozen.json",
        "Proposer": "proposal-transfer-v2/frozen.json",
        "Seam": "seam-location/frozen.json",
        "Mirror": "mirror-followup/winner-lock.json",
    }
    for binding, file in gate_files.items():
        check_hash(folder / file, gate[binding])
    for file, digest in recorded(primary / "protocol.lock.json")["Manifest"].items():
        check_hash(primary / "manifest" / file, digest)
    for directory in ("primary/test", "proposal-transfer-v2/reserve", "seam-location/reserve", "mirror-followup/reserve"):
        run = folder / directory
        seal = recorded(run / "complete.json")
        for file, digest in (seal.get("FileHashes") or seal["Files"]).items():
            check_hash(run / file, digest)
    mirror = folder / "mirror-followup"
    check_hash(mirror / "winners.json", recorded(mirror / "winner-lock.json")["WinnerFile"])
    ref = recorded(primary / "replay/verification.json")
    for file in ("expected.json", "observed.json"):
        check_hash(primary / "replay" / file, ref["ReferenceSha256"])
    exchange = folder / "owner-exchange"
    transfer = recorded(exchange / "complete.json")
    check_hash(exchange / "panels.csv", transfer["PanelHash"])
    check_hash(exchange / "owners.csv", transfer["OwnerHash"])
    verification = folder / "verification"
    census = recorded(verification / "vocabulary-census.json")
    check_hash(primary / "frozen.json", census["input_sha256"])
    check_hash(verification / "vocabulary-census.csv", census["census_sha256"])

    idle = {}
    for row in rows(primary / "manifest/episodes.csv"):
        if row["split"] == "test" and row["goal"] == "infill":
            material = int(row["initial_hex"], 16) ^ int(row["intervention_xor_hex"], 16)
            idle[row["parent"], row["variant"]] = material.bit_count()
    changes = {"open": [], "evolve": []}
    for row in rows(primary / "test/cases.csv"):
        if (row["training_goal"], row["goal"], row["wire"], row["treatment"]) != ("infill", "infill", "Live", "native"):
            continue
        if row["method"] in changes:
            final_count = int(row["final_ink_hex"], 16).bit_count()
            changes[row["method"]].append(final_count - idle[row["group"], row["variant"]])
    return {method: {"cases": len(deltas), "fewer": sum(n < 0 for n in deltas),
                     "same": deltas.count(0), "more": sum(n > 0 for n in deltas),
                     "mean_change": sum(deltas) / len(deltas)}
            for method, deltas in changes.items()}


if __name__ == "__main__":
    print(json.dumps(audit(Path(sys.argv[1])), indent=2))
