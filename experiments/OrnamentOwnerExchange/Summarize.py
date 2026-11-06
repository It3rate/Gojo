"""Verify the owner grid, then summarize material and utility separately."""
import argparse
import csv
import hashlib
import json
from collections import defaultdict
from pathlib import Path


def read_rows(path):
    with path.open(encoding="utf-8-sig", newline="") as source:
        return list(csv.DictReader(source))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def summary(folder):
    lock = json.loads((folder / "complete.json").read_text())
    for key, name in (("OwnerHash", "owners.csv"), ("PanelHash", "panels.csv")):
        digest = hashlib.sha256((folder / name).read_bytes()).hexdigest().upper()
        require(digest == lock[key], name + " hash changed")
    owners = {r["owner"]: r for r in read_rows(folder / "owners.csv")}
    rows = read_rows(folder / "panels.csv")
    text = {"writer", "donor", "goal", "program_hex", "material_digest"}
    for r in rows:
        for k in r.keys() - text:
            r[k] = int(r[k])

    goals = ["balance", "infill", "mirror", "seam"]
    expected = {(w, s, g) for w in owners for s in owners for g in goals
                if w.split("-")[0] in (g, "mixed")}
    require(len(owners) == 40 and len(expected) == len(rows) == 2560, "Incomplete experiment")
    require({(r["writer"], r["donor"], r["goal"]) for r in rows} == expected, "Invalid crossings")
    classes = defaultdict(set)
    programs = {}
    controls = defaultdict(set)
    for r in rows:
        require(r["cases"] == 144 and r["parents"] == 72 and r["violations"] == 0, "Invalid panel")
        controls[r["writer"], r["goal"]].add((r["sum_original"], r["sum_cut"], r["sum_idle"]))
        classes[r["writer"], r["goal"]].add(r["material_digest"])
        key = (r["program_hex"], r["goal"])
        observation = (r["material_digest"], r["sum_utility"])
        require(programs.setdefault(key, observation) == observation, "An identical program produced a different read")
        if r["writer"] == r["donor"]:
            require(r["program_hex"] == owners[r["writer"]]["genome_hex"], "Diagonal genome")
            require(r["changed_original"] == r["trace_changed_original"] == 0, "Diagonal material/trace")
            require(r["sum_utility"] == r["sum_original"], "Diagonal utility")
    require(all(len(c) == 1 for c in controls.values()), "Control changed with donor")
    foreign = [r for r in rows if r["writer"] != r["donor"]]
    is_strong = lambda r: (r["improved_original"] >= 48 and r["improved_cut"] >= 48
                            and r["sum_utility"] > r["sum_idle"])
    report = {}
    for g in ["all", *goals]:
        a = [r for r in foreign if g in ("all", r["goal"])]
        good = [r for r in a if is_strong(r)]
        report[g] = {
            "foreign_panels": len(a),
            "altered_material_vs_original": sum(r["changed_original"] > 0 for r in a),
            "altered_material_vs_cut": sum(r["changed_cut"] > 0 for r in a),
            "different_trace_only_vs_original": sum(r["changed_original"] == 0 and r["trace_changed_original"] > 0 for r in a),
            "decisively_better_than_original": sum(r["improved_original"] >= 48 for r in a),
            "decisively_worse_than_original": sum(r["lost_original"] >= 48 for r in a),
            "strong_reuse": len(good),
            "strong_donor_byte_tables": len({owners[r["donor"]]["sensor_with_idle_writer_hex"] for r in good}),
            "strong_writers": sorted({r["writer"] for r in good}),
        }
    report["material_response_classes_per_writer_brief"] = {
        f"{w}/{g}": len(signatures) for (w, g), signatures in sorted(classes.items())}
    report["used_distinct_sensor_tables"] = len({o["sensor_with_idle_writer_hex"] for o in owners.values()})
    report["strong_transplants"] = [{k: r[k] for k in (
        "writer", "donor", "goal", "program_hex", "material_digest", "sum_utility", "sum_original",
        "sum_cut", "sum_idle", "improved_original", "improved_cut")} for r in foreign if is_strong(r)]
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder", type=Path)
    folder = parser.parse_args().folder
    (folder / "summary.json").write_text(json.dumps(summary(folder), indent=2) + "\n", encoding="utf-8")
