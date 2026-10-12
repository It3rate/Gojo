"""Read-only analysis of CompositeEmergence artifacts; Python standard library.

Seed identities, not episodes or checkpoints, are the paired replicate unit.
This script projects diagnostic artifacts. It does not operate a Gojo learner.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from collections import Counter, defaultdict
from pathlib import Path
from statistics import mean, stdev


T95 = {
    1: 12.706204736, 2: 4.302652730, 3: 3.182446305, 4: 2.776445105,
    5: 2.570581836, 6: 2.446911849, 7: 2.364624252, 8: 2.306004135,
    9: 2.262157163, 10: 2.228138852, 11: 2.200985160, 12: 2.178812830,
    13: 2.160368656, 14: 2.144786688, 15: 2.131449546, 16: 2.119905299,
    17: 2.109815578, 18: 2.100922040, 19: 2.093024054, 20: 2.085963447,
    21: 2.079613845, 22: 2.073873068, 23: 2.068657610, 24: 2.063898562,
    25: 2.059538553, 26: 2.055529439, 27: 2.051830516, 28: 2.048407142,
    29: 2.045229642, 30: 2.042272456,
}


def normalize(value):
    if isinstance(value, list):
        return [normalize(item) for item in value]
    if isinstance(value, dict):
        return {key[:1].lower() + key[1:]: normalize(item) for key, item in value.items()}
    return value


def read(path):
    return normalize(json.loads(path.read_text(encoding="utf-8-sig")))


def estimate(values):
    values = list(values)
    if not values:
        return {"n": 0, "mean": None, "low95": None, "high95": None}
    result = {"n": len(values), "mean": mean(values), "low95": None, "high95": None,
              "minimum": min(values), "maximum": max(values)}
    if len(values) > 1:
        # Larger runs use the conservative df30 critical value, explicitly
        # recorded below, rather than silently treating a small run as normal.
        critical = T95[min(len(values) - 1, 30)]
        error = critical * stdev(values) / math.sqrt(len(values))
        result.update(low95=result["mean"] - error, high95=result["mean"] + error,
                      criticalValue=critical)
    return result


def key(row):
    return row["stage"], row["variant"], row["condition"], row["trainEpisodes"]


def fmt(stat, percentage=False):
    scale = 100 if percentage else 1
    suffix = "%" if percentage else ""
    if stat["mean"] is None:
        return "unavailable"
    value = f'{stat["mean"] * scale:.3f}{suffix}'
    if stat["low95"] is not None:
        value += f' [{stat["low95"] * scale:.3f}, {stat["high95"] * scale:.3f}]'
    return value


def analyze(directory):
    data = read(directory / "data.json")
    rows = data["results"]
    settings = data["settings"]
    run = read(directory / "run.json") if (directory / "run.json").exists() else {}
    manifest = read(directory / "manifest.json")
    warnings = []
    if data.get("status") != "complete" or run.get("status") != "complete":
        warnings.append("Run is incomplete; these artifacts are development evidence.")
    if settings.get("pilot"):
        warnings.append("Pilot seed data: do not present as fresh confirmation.")
    unique_rows = set()
    checked_resources = 0
    for row in rows:
        identity = (*key(row), row["seed"])
        if identity in unique_rows:
            warnings.append(f"Duplicate result identity: {identity}")
        unique_rows.add(identity)
        n = row["testEpisodes"]
        if n <= 0:
            warnings.append(f"Nonpositive episode count: {identity}")
            continue
        for field, count in (("accuracy", "correct"), ("coverage", "answered"), ("queryRate", "queries")):
            if not math.isclose(row[field], row[count] / n, abs_tol=1e-12):
                warnings.append(f"Rate/count mismatch {field}: {identity}")
        if not math.isclose(row["meanReturn"], row["returnSum"] / n, abs_tol=1e-12):
            warnings.append(f"Resource mean mismatch: {identity}")
        if not 0 <= row["correct"] <= row["answered"] <= n:
            warnings.append(f"Coverage/correctness mismatch: {identity}")
        if row["stage"] in ("source", "transfer", "refresh"):
            expected = 8 * row["correct"] - 12 * (row["answered"] - row["correct"])
            if row["stage"] == "refresh":
                price = 5 if row["condition"].endswith("expensive-probe") else settings["probePrice"]
                expected -= price * row["queries"]
            if expected != row["returnSum"]:
                warnings.append(f"Resource accounting mismatch: {identity}, expected {expected}, got {row['returnSum']}")
            checked_resources += 1

    groups = defaultdict(list)
    for row in rows:
        groups[key(row)].append(row)
    summaries = []
    for group_key, members in sorted(groups.items()):
        stage, variant, condition, trained = group_key
        summary = {"stage": stage, "variant": variant, "condition": condition,
                   "trainEpisodes": trained, "seeds": sorted(member["seed"] for member in members)}
        for metric in ("accuracy", "coverage", "queryRate", "meanReturn", "unrecovered", "switches", "featureStates", "candidateCount"):
            # The runner does not record feature-state/candidate counts for
            # temporal evaluations; their schema default zero is not a read.
            summary[metric] = estimate([] if stage == "refresh" and metric in ("featureStates", "candidateCount")
                                       else (member.get(metric, 0) for member in members))
        completed = [member for member in members if member.get("switches", 0) - member.get("unrecovered", 0) > 0]
        summary["resolvedIntervals"] = estimate(member.get("switches", 0) - member.get("unrecovered", 0) for member in members)
        summary["meanRecovery"] = estimate(member["meanRecovery"] for member in completed)
        summary["recoverySeedsWithoutResolvedIntervals"] = sorted(member["seed"] for member in members if member not in completed)
        summary["conditionalAccuracy"] = estimate(member["correct"] / member["answered"]
                                                   for member in members if member["answered"] > 0)
        summary["selectedDescriptions"] = dict(Counter(member["selectedDescription"] for member in members))
        summary["graph"] = {metric: estimate(member.get("graph", {}).get(metric, 0) for member in members)
                            for metric in ("carriers", "relations", "placeholders", "coordinates")}
        summaries.append(summary)

    contrasts = []

    def compare(stage, left, right, condition, trained, metric):
        first = {row["seed"]: row for row in groups.get((stage, left, condition, trained), [])}
        second = {row["seed"]: row for row in groups.get((stage, right, condition, trained), [])}
        seeds = sorted(set(first) & set(second))
        if not seeds:
            return
        differences = {seed: first[seed][metric] - second[seed][metric] for seed in seeds}
        contrasts.append({"stage": stage, "left": left, "right": right, "condition": condition,
                          "trainEpisodes": trained, "metric": metric, "seeds": seeds,
                          "difference": estimate(differences.values()),
                          "perSeed": [{"seed": seed, "left": first[seed][metric], "right": second[seed][metric],
                                       "difference": differences[seed]} for seed in seeds]})

    discovery_train = settings["discoveryTrain"]
    for variant in ("no-expansion", "frozen", "shuffled-feedback", "shuffled-communication", "no-memory", "previous-pair"):
        for metric in ("accuracy", "coverage"):
            compare("discovery", "learned", variant, "fresh-rule", discovery_train, metric)
    consumer_train = settings["consumerTrain"]
    for variant in ("no-expansion", "frozen", "shuffled-feedback", "shuffled-communication", "no-memory", "previous-pair", "flat", "fixed-sum"):
        for metric in ("accuracy", "meanReturn"):
            compare("source", "learned", variant, "baseline", consumer_train, metric)
    checkpoints = sorted({row["trainEpisodes"] for row in rows if row["stage"] == "transfer"})
    for checkpoint in checkpoints:
        for variant in ("fresh-service", "no-expansion", "shuffled-communication", "flat", "fixed-sum"):
            for metric in ("accuracy", "meanReturn"):
                compare("transfer", "reused-service", variant, "baseline", checkpoint, metric)
    temporal_conditions = sorted({row["condition"] for row in rows if row["stage"] == "refresh"})
    for condition in temporal_conditions:
        for variant in ("always-probe", "never-probe", "outcome-memory", "failure-probe", "fixed-interval"):
            for metric in ("accuracy", "meanReturn", "queryRate"):
                compare("refresh", "learned-refresh", variant, condition, settings["refreshTrain"], metric)

    shift_contrasts = []
    for condition in ("long-delay", "missing-signals", "opposite-state", "same-state", "absent-state", "inverted-input"):
        first = {row["seed"]: row for row in groups.get(("source", "learned", condition, consumer_train), [])}
        baseline = {row["seed"]: row for row in groups.get(("source", "learned", "baseline", consumer_train), [])}
        seeds = sorted(set(first) & set(baseline))
        if seeds:
            shift_contrasts.append({"condition": condition, "seeds": seeds,
                                   "accuracyMinusBaseline": estimate(first[seed]["accuracy"] - baseline[seed]["accuracy"] for seed in seeds),
                                   "returnMinusBaseline": estimate(first[seed]["meanReturn"] - baseline[seed]["meanReturn"] for seed in seeds)})

    # These milestones are measured on frozen checkpoint evaluations. A seed
    # that never reaches the criterion remains censored, rather than becoming
    # a fabricated large training count in a mean.
    attainment = []
    for criterion in (.95, 1.0):
        for variant in sorted({row["variant"] for row in rows if row["stage"] == "transfer"}):
            variant_rows = [row for row in rows if row["stage"] == "transfer" and row["variant"] == variant and row["condition"] == "baseline"]
            first = []
            for seed in sorted({row["seed"] for row in variant_rows}):
                qualifying = [row["trainEpisodes"] for row in variant_rows if row["seed"] == seed and row["accuracy"] >= criterion]
                first.append({"seed": seed, "firstMeasuredMilestone": min(qualifying) if qualifying else None})
            attainment.append({"variant": variant, "criterion": criterion, "perSeed": first,
                               "reached": sum(item["firstMeasuredMilestone"] is not None for item in first),
                               "total": len(first)})

    repository = Path(__file__).resolve().parents[2]
    hash_checks = []
    for relative, expected in manifest.get("sourceHashes", {}).items():
        path = repository / relative
        actual = hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
        hash_checks.append({"path": relative, "match": actual == expected})
    if not all(check["match"] for check in hash_checks):
        warnings.append("Some current source hashes differ from the run manifest; preserved run data remains historical.")
    archives = data.get("archives", [])
    if any(not item.get("signatureMatch") or not item.get("behaviorMatch") for item in archives):
        warnings.append("One or more native archive checks did not match.")
    # Read inspection projections separately from total runner graph counts.
    # Flat/fixed-sum runner roots include a common source model, so those totals
    # are not necessary-controller footprint comparisons.
    consumer_contexts = []
    for path in sorted(directory.glob("consumer-*.json")):
        projection = read(path)
        evidence = projection.get("evidence", [])
        consumer_contexts.append({"path": path.name, "seed": projection.get("seed"),
                                  "stage": projection.get("stage"), "variant": projection.get("variant"),
                                  "trained": projection.get("trained"),
                                  "uniqueContexts": len({tuple(row["context"]) for row in evidence}),
                                  "offerLedgers": len(evidence)})
    manager_projections = []
    for path in sorted(directory.glob("refresh-*-*.json")):
        projection = read(path)
        manager_projections.append({"path": path.name, "seed": projection.get("seed"),
                                    "historyDelay": projection.get("historyDelay"),
                                    "rowCount": projection.get("rowCount"),
                                    "updateCount": projection.get("updateCount"),
                                    "agingEvents": projection.get("agingEvents")})
    return {"directory": str(directory.resolve()), "status": data.get("status"), "settings": settings,
            "startedUtc": data.get("startedUtc"), "finishedUtc": data.get("finishedUtc"), "elapsedSeconds": data.get("elapsedSeconds"),
            "counts": {"uniqueRows": len(unique_rows), "savedTestEvaluations": sum(row["testEpisodes"] for row in rows),
                       "runnerTrainingEvents": run.get("trainingEvents"), "runnerEvaluations": run.get("evaluations"),
                       "resourceIdentitiesChecked": checked_resources, "sourceHashesChecked": len(hash_checks)},
            "summaries": summaries, "pairedContrasts": contrasts, "shiftContrasts": shift_contrasts,
            "transferAttainment": attainment, "archives": archives, "sourceHashChecks": hash_checks,
            "consumerContexts": consumer_contexts, "managerProjections": manager_projections,
            "discoveryCandidates": data.get("discoveryCandidates", []), "warnings": warnings,
            "intervalMethod": "two-sided 95% Student t on seed means or paired seed differences; df>30 uses conservative df30 critical; exploratory contrasts without multiplicity correction",
            "recoveryMeaning": "seed mean time to first successful action after a switch conditional on at least one completed interval; zero sentinel excluded when switches minus unrecovered is zero; interrupted or unresolved intervals are separate, not sustained recovery"}


def markdown(studies):
    lines = ["# Composite emergence: generated analysis", "",
             "Seed-paired descriptive intervals. Checkpoints are separate groups; repeated episodes are not independent replicates.", "",
             "Source/transfer acquisitions are counted without a probe fee. Refresh acquisitions are paid. Recovery is first-success delay, not sustained recovery.", ""]
    for study in studies:
        lines += [f"## {Path(study['directory']).name}", "", f"Status: {study['status']}. Settings: `{json.dumps(study['settings'], sort_keys=True)}`.", "",
                  f"Counts: `{json.dumps(study['counts'], sort_keys=True)}`.", ""]
        for warning in study["warnings"]:
            lines += [f"- {warning}"]
        if study["warnings"]:
            lines.append("")
        lines += ["| Stage | Controller | Condition | Train | Seeds | Accuracy | Coverage | Acquisitions | Return |",
                  "|---|---|---|---:|---:|---|---|---|---|"]
        for row in study["summaries"]:
            lines.append(f"| {row['stage']} | {row['variant']} | {row['condition']} | {row['trainEpisodes']} | {len(row['seeds'])} | {fmt(row['accuracy'], True)} | {fmt(row['coverage'], True)} | {fmt(row['queryRate'], True)} | {fmt(row['meanReturn'])} |")
        lines += ["", "### Paired effects", "",
                  "| Stage | Left minus right | Condition | Train | Metric | Seeds | Difference |",
                  "|---|---|---|---:|---|---:|---|"]
        for row in study["pairedContrasts"]:
            lines.append(f"| {row['stage']} | {row['left']} minus {row['right']} | {row['condition']} | {row['trainEpisodes']} | {row['metric']} | {len(row['seeds'])} | {fmt(row['difference'], row['metric'] in ('accuracy', 'coverage', 'queryRate'))} |")
        lines += ["", "### Conditional first success after switches", "",
                  "Means give each eligible seed equal weight. A seed with no completed interval is omitted from delay, not treated as instant recovery. Interrupted or unresolved intervals remain in the count columns. No sustained recovery is measured.", "",
                  "| Controller | Condition | Delay-eligible seeds | Mean first-success delay | Mean completed intervals | Mean unresolved/interrupted intervals | Omitted seeds |",
                  "|---|---|---:|---|---|---|---|"]
        for row in study["summaries"]:
            if row["stage"] == "refresh":
                lines.append(f"| {row['variant']} | {row['condition']} | {row['meanRecovery']['n']} | {fmt(row['meanRecovery'])} | {fmt(row['resolvedIntervals'])} | {fmt(row['unrecovered'])} | {row['recoverySeedsWithoutResolvedIntervals']} |")
        lines += ["", "### Native archives", ""]
        for archive in study["archives"]:
            lines.append(f"- {archive['model']}: {archive['bytes']:,} bytes; signature={archive['signatureMatch']}, behavior={archive['behaviorMatch']}; `{archive['path']}`.")
        lines += ["", f"Interval method: {study['intervalMethod']}.", ""]
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directories", nargs="+", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    studies = [analyze(directory) for directory in args.directories]
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "analysis.json").write_text(json.dumps({"studies": studies}, indent=2, allow_nan=False), encoding="utf-8")
    (args.output / "analysis.md").write_text(markdown(studies), encoding="utf-8")
    print(json.dumps({"output": str(args.output.resolve()), "studies": [{"directory": study["directory"], "counts": study["counts"], "warnings": study["warnings"]} for study in studies]}, indent=2))


if __name__ == "__main__":
    main()
