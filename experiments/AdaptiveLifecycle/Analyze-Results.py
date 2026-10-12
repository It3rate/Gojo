"""Analyze recorded native trials. Python standard library; no learning model."""
import argparse
import hashlib
import json
import math
import statistics
from pathlib import Path


def estimate(values):
    values = list(values)
    if not values:
        return {"n": 0, "mean": None, "low95": None, "high95": None}
    mean = statistics.mean(values)
    critical = {2: 12.70620474, 3: 4.30265273, 4: 3.182446305,
                5: 2.776445105, 6: 2.570581836, 7: 2.446911851,
                8: 2.364624252, 9: 2.306004135}.get(len(values))
    margin = None if critical is None else critical * statistics.stdev(values) / math.sqrt(len(values))
    return {"n": len(values), "mean": mean,
            "low95": None if margin is None else mean - margin,
            "high95": None if margin is None else mean + margin,
            "minimum": min(values), "maximum": max(values)}


def analyze(directory):
    manifest = json.loads((directory / "manifest.json").read_text())
    run = json.loads((directory / "run.json").read_text())
    files = sorted(directory.glob("seed-*.json"))
    records = [json.loads(path.read_text()) for path in files]
    warnings = []
    checked_rows = checked_steps = 0
    for record in records:
        arms = {row["arm"]: row for row in record["rows"]}
        traces = {arm: [step for step in record["trace"] if step["arm"] == arm] for arm in arms}
        private_tapes = [tuple(step["privateRule"] for step in traces[arm]) for arm in arms]
        if any(tape != private_tapes[0] for tape in private_tapes):
            warnings.append(f"Unpaired world tape seed {record['seed']}")
        for arm, row in arms.items():
            steps = traces[arm]
            checks = [len(steps) == row["trials"],
                      sum(step["correct"] for step in steps) == row["correct"],
                      sum(not step["hold"] for step in steps) == row["answered"],
                      sum(step["probe"] for step in steps) == row["queries"],
                      sum(step["netResource"] for step in steps) == row["netResource"],
                      row["grossResource"] - row["probeDebits"] == row["netResource"],
                      row["probeDebits"] == row["queries"] * record["settings"]["probePrice"],
                      row["grossResource"] == 8 * row["correct"] - 12 * (row["answered"] - row["correct"]),
                      row["lowerSignatureBefore"] == row["lowerSignatureAfter"]]
            online = arm.endswith("/online")
            expected_updates = row["trials"] if online else 0
            checks.append(row["managerUpdatesAfter"] - row["managerUpdatesBefore"] == expected_updates)
            if not online:
                checks.extend([row["managerSignatureBefore"] == row["managerSignatureAfter"],
                               row["managerAgesBefore"] == row["managerAgesAfter"]])
            if not all(checks):
                warnings.append(f"Row verification failed: seed {record['seed']} {arm}")
            checked_rows += 1
            for step in steps:
                if (step["grossResource"] - step["probeDebit"] != step["netResource"] or
                    (not step["probe"] and (step["probeMissingA"] is not None or step["probeMissingB"] is not None)) or
                    (step["hold"] and (step["feature"] is not None or step["availableAfter"]))):
                    warnings.append(f"Step verification failed: seed {record['seed']} {arm} {step['index']}")
                checked_steps += 1
        frozen, online = arms["clean-trained/frozen"], arms["clean-trained/online"]
        if frozen["managerSignatureBefore"] != online["managerSignatureBefore"]:
            warnings.append(f"Initial models not paired: seed {record['seed']}")
    if len(records) != manifest["seeds"] or run["status"] != "complete":
        warnings.append("Run is incomplete or expected seeds missing")
    hashes_checked = 0
    repository = next((parent for parent in directory.resolve().parents if (parent / "Gojo.slnx").exists()), None)
    if repository:
        for relative, expected in manifest["sourceHashes"].items():
            path = repository / relative
            hashes_checked += 1
            if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
                warnings.append(f"Current source differs from recorded source: {relative}")
    else:
        warnings.append("Cannot verify current source hashes")
    summaries = []
    arms = sorted({row["arm"] for record in records for row in record["rows"]})
    for arm in arms:
        rows = [row for record in records for row in record["rows"] if row["arm"] == arm]
        summary = {"arm": arm, "seeds": [row["seed"] for row in rows]}
        for metric in ("accuracy", "coverage", "queryRate", "meanResource", "longestHoldRun", "trailingHoldRun"):
            summary[metric] = estimate(row[metric] for row in rows)
        summary["failedProbesRecovered"] = sum(row["failedProbesRecovered"] for row in rows)
        summary["failedProbesUnrecovered"] = sum(row["failedProbesUnrecovered"] for row in rows)
        summary["conditionalFirstPublicationDelay"] = estimate(row["meanProbeRecovery"] for row in rows if row["meanProbeRecovery"] is not None)
        summary["extraManagerUpdates"] = [row["managerUpdatesAfter"] - row["managerUpdatesBefore"] for row in rows]
        summary["newContextRows"] = [row["managerRowsAfter"] - row["managerRowsBefore"] for row in rows]
        summaries.append(summary)
    pairs = [("clean-trained/online", "clean-trained/frozen"),
             ("missing-trained/frozen", "clean-trained/frozen"),
             ("missing-trained/online", "missing-trained/frozen"),
             ("clean-trained/online", "retry-if-unavailable"),
             ("missing-trained/frozen", "retry-if-unavailable")]
    comparisons = []
    for first, second in pairs:
        differences = {metric: [] for metric in ("accuracy", "coverage", "queryRate", "meanResource")}
        for record in records:
            indexed = {row["arm"]: row for row in record["rows"]}
            for metric in differences:
                differences[metric].append(indexed[first][metric] - indexed[second][metric])
        comparisons.append({"first": first, "second": second,
                            **{metric: estimate(values) for metric, values in differences.items()}})
    archives_path = directory / "archive-check.json"
    archives = json.loads(archives_path.read_text()) if archives_path.exists() else []
    for archive in archives:
        if not archive["signatureMatched"] or not archive["behaviorMatched"]:
            warnings.append(f"Failed actual archive replay: {archive['filename']}")
    lifecycle = json.loads((directory / "lifecycle.json").read_text())
    if not all(lifecycle[key] for key in ("dormantWithdraws", "debtPreserved", "freshIdentity", "capacityBlocksBirth",
                                       "fundingBlocksBirth", "fundedParticipantSurvives", "transportPreservesState")):
        warnings.append("A deterministic lifecycle assertion failed")
    if lifecycle["birthCredits"] + lifecycle["remainingNurseryFunding"] != lifecycle["initialNurseryFunding"]:
        warnings.append("Nursery endowment funding identity failed")
    if "maintenancePaid" in lifecycle and (lifecycle["initialNurseryFunding"] + lifecycle["externalCredits"] !=
                                           lifecycle["remainingNurseryFunding"] + lifecycle["maintenancePaid"] or
                                           lifecycle["maintenanceRequested"] - lifecycle["retiredShortfall"] != lifecycle["maintenancePaid"]):
        warnings.append("Lifecycle maintenance identity failed")
    return {"directory": str(directory.resolve()), "settings": manifest["settings"], "campaign": manifest["campaign"],
            "elapsedSeconds": run["elapsedSeconds"], "seedCount": len(records), "rowsChecked": checked_rows,
            "stepsChecked": checked_steps, "sourceHashesChecked": hashes_checked, "archiveChecks": archives,
            "summaries": summaries, "pairedComparisons": comparisons, "lifecycle": lifecycle,
            "warnings": warnings, "paidJevCalls": run["paidJevCalls"]}


def markdown(result):
    lines = ["# Continuous adaptation analysis", "", f"{result['seedCount']} seeds; {result['stepsChecked']:,} evaluation decisions; {result['elapsedSeconds']:.1f} seconds.",
             "", "Means across seeds. Intervals are descriptive Student-t 95% intervals over seeds, not individual steps. Online arms receive extra feedback during evaluation. No multiple-comparison correction.",
             "", "| Arm | Correct | Action coverage | Paid probes | Net return | Longest hold (mean) |", "|---|---:|---:|---:|---:|---:|"]
    for row in result["summaries"]:
        lines.append(f"| {row['arm']} | {row['accuracy']['mean']:.2%} | {row['coverage']['mean']:.2%} | {row['queryRate']['mean']:.2%} | {row['meanResource']['mean']:.3f} | {row['longestHoldRun']['mean']:.1f} |")
    lines += ["", "## Paired contrasts", ""]
    for contrast in result["pairedComparisons"]:
        metric = contrast["meanResource"]
        interval = "unavailable" if metric["low95"] is None else f"[{metric['low95']:.3f}, {metric['high95']:.3f}]"
        lines.append(f"- {contrast['first']} minus {contrast['second']}: net return {metric['mean']:+.3f}, 95% interval {interval}; coverage {contrast['coverage']['mean']:+.2%}.")
    lines += ["", "## Verification", "", f"{result['rowsChecked']} rows, {result['stepsChecked']:,} steps, {result['sourceHashesChecked']} current source hashes, {len(result['archiveChecks'])} actual saved archive checks.",
              "", "First-publication recovery is conditional and does not establish sustained useful recovery. A new failed probe supersedes the prior interval as unresolved. See JSON for denominators and eligible-seed counts.", "", "## Warnings", ""]
    lines += ["- " + warning for warning in result["warnings"]] if result["warnings"] else ["None."]
    return "\n".join(lines) + "\n"


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    result = analyze(args.directory)
    destination = args.directory / "analysis"
    destination.mkdir(exist_ok=True)
    (destination / "analysis.json").write_text(json.dumps(result, indent=2) + "\n")
    (destination / "analysis.md").write_text(markdown(result), encoding="utf-8")
    print(markdown(result))
