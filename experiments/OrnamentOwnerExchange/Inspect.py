"""Build the standalone owner-exchange inspector from the completed local run."""

import argparse
import csv
import hashlib
import json
from pathlib import Path


def load_verified_csv(folder, name, expected_hash):
    path = folder / name
    if hashlib.sha256(path.read_bytes()).hexdigest().upper() != expected_hash.upper():
        raise ValueError(f"{name} does not match the completed exchange")
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def build(folder):
    complete = json.loads((folder / "complete.json").read_text(encoding="utf-8-sig"))
    owners = load_verified_csv(folder, "owners.csv", complete["OwnerHash"])
    panels = load_verified_csv(folder, "panels.csv", complete["PanelHash"])
    goals = ["balance", "infill", "mirror", "seam"]
    training = goals + ["mixed"]

    def owner_order(row):
        goal, method, replicate = row["owner"].split("-")
        return (training.index(goal), ("evolve", "random").index(method), int(replicate))

    owners.sort(key=owner_order)
    ranks = {owner["owner"]: i for i, owner in enumerate(owners)}
    fields = ["sum_utility", "sum_original", "sum_cut", "sum_idle",
              "improved_original", "lost_original", "improved_cut", "lost_cut",
              "changed_original", "changed_cut", "trace_changed_original", "cases",
              "parents", "violations", "program_hex"]
    views = {goal: {} for goal in goals}
    for row in panels:
        matrix = views[row["goal"]]
        writer = ranks[row["writer"]]
        donor = ranks[row["donor"]]
        sensors = matrix.setdefault(writer, [None] * len(owners))
        if sensors[donor] is not None:
            raise ValueError("Duplicate panel for the same brief, writer and donor")
        sensors[donor] = [int(row[column]) if column != "program_hex" else row[column]
                          for column in fields]
    if (len(ranks) != len(owners) or len(panels) != complete["EvaluatedPanels"]
            or any(not sensors or None in sensors for matrix in views.values()
                   for sensors in matrix.values())):
        raise ValueError("The exchange matrix has missing panels")
    payload = json.dumps(dict(owners=owners, views=views, goals=goals, fields=fields,
                              cases=complete["TransplantEpisodes"]), separators=(",", ":"))
    html = Path(__file__).with_name("Inspector.html").read_text(encoding="utf-8")
    (folder / "Explore.html").write_text(html.replace("/*__PAYLOAD__*/", payload.replace("<", "\\u003c")),
                                        encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder", nargs="?", type=Path,
                        default=Path(__file__).resolve().parents[2] / "artifacts/local-ornament/owner-exchange")
    build(parser.parse_args().folder)
