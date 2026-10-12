"""Read-only analysis of raw runs; derived outputs are separate from run evidence.
Python standard library only. No simulator or learner parameter is changed.
"""
import argparse
import csv
import hashlib
import json
import math
from pathlib import Path
import statistics
import zipfile


def estimate(values):
    mean = statistics.mean(values)
    sd = statistics.stdev(values) if len(values) > 1 else 0.0
    # Frozen protocol uses eight seed means, t(7)=2.364624. These are descriptive
    # unadjusted intervals, not a simultaneous ten-arm significance claim.
    radius = 2.364624 * sd / math.sqrt(len(values)) if len(values) == 8 else None
    return {"n": len(values), "mean": mean, "sd": sd, "min": min(values), "max": max(values),
            "ci95": [mean - radius, mean + radius] if radius is not None else None, "values": values}


def phase_metrics(run, phase, source, first=None, last=None):
    trace = [step for step in run["trace"] if step["phase"] == phase and step["source"] == source]
    if first is not None:
        trace = trace[:first]
    if last is not None:
        trace = trace[-last:]
    answered = sum(step["action"] != 0 for step in trace)
    correct = sum(step["correct"] for step in trace)
    return {"steps": len(trace), "answered": answered, "correct": correct,
            "accuracy": correct / answered if answered else None,
            "netPerStep": sum(step["gross"] - step["burned"] for step in trace) / len(trace) if trace else None,
            "interpreted": sum(step["interpreted"] for step in trace),
            "grants": sum(step["learningGranted"] for step in trace),
            "refinements": sum(step["refined"] for step in trace),
            "resets": sum(step["reset"] for step in trace),
            "burned": sum(step["burned"] for step in trace),
            "presenceActions": sum(step["interpreted"] and not step["refined"] and not step["reset"] and step["landmarks"] == 0 for step in trace),
            "ordinalActions": sum(step["interpreted"] and not step["refined"] and not step["reset"] and step["landmarks"] > 0 for step in trace)}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", nargs="?", default="results/confirmation-01")
    args = parser.parse_args()
    here = Path(__file__).resolve().parent
    directory = Path(args.directory)
    if not directory.is_absolute():
        directory = here / directory
    manifest = json.loads((directory / "manifest.json").read_text())
    completion = json.loads((directory / "run.json").read_text())
    assert completion["status"] == "complete", "Do not summarize an incomplete campaign"
    names = [arm["name"] for arm in manifest["arms"]]
    runs = [json.loads(path.read_text()) for path in sorted(directory.glob("*.json"))
            if path.name.split("-", 1)[0].isdigit()]
    assert len(runs) == manifest["seeds"] * len(names)
    seeds = list(range(manifest["seedStart"], manifest["seedStart"] + manifest["seeds"]))
    indexed = {(run["seed"], run["arm"]): run for run in runs}
    count = manifest["settings"]["phaseLength"] * 4
    for run in runs:
        assert run["settings"] == manifest["settings"]
        assert len(run["trace"]) == count
        assert sum(step["gross"] for step in run["trace"]) == run["gross"]
        assert sum(step["burned"] for step in run["trace"]) == run["burned"]
        assert sum(row["gross"] for row in run["rows"]) == run["gross"]
        assert sum(row["burned"] for row in run["rows"]) == run["burned"]
        assert abs((run["gross"] - run["burned"]) / count - run["netPerStep"]) < 1e-12
        assert run["accountedMoney"] == run["externalNet"] == manifest["settings"]["initialCapital"] + run["gross"]
        gross = 0
        for step in run["trace"]:
            gross += step["gross"]
            assert step["accountedMoney"] == step["externalNet"] == manifest["settings"]["initialCapital"] + gross
            assert step["correct"] == (step["gross"] > 0)
            assert step["action"] != 0 or step["gross"] == 0
    for seed in seeds:
        assert len({indexed[seed, name]["tapeDigest"] for name in names}) == 1
        assert len({tuple((step["phase"], step["source"]) for step in indexed[seed, name]["trace"]) for name in names}) == 1
    archives = json.loads((directory / "archive-check.json").read_text())
    assert len(archives) == len(names)
    assert all(a["signatureMatched"] and a["monetaryMatched"] and a["aliasesMatched"] and
               a["behavior"]["matched"] and a["behavior"]["continuedMoneyConserved"] for a in archives)
    repository = here.parents[1]
    for filename, expected in manifest["sourceHashes"].items():
        assert hashlib.sha256((repository / filename).read_bytes()).hexdigest() == expected, filename
    summary = {}
    for name in names:
        records = [indexed[seed, name] for seed in seeds]
        total_answered = sum(sum(step["action"] != 0 for step in r["trace"]) for r in records)
        total_correct = sum(sum(step["correct"] for step in r["trace"]) for r in records)
        phases = []
        for phase in range(4):
            for source in range(4):
                measurements = [phase_metrics(r, phase, source) for r in records]
                if not any(m["steps"] for m in measurements):
                    continue
                answered = sum(m["answered"] for m in measurements)
                correct = sum(m["correct"] for m in measurements)
                phases.append({"phase": phase, "source": source,
                    "steps": sum(m["steps"] for m in measurements), "answered": answered,
                    "accuracy": correct / answered if answered else None,
                    "netPerStep": estimate([m["netPerStep"] for m in measurements if m["netPerStep"] is not None]),
                    "grants": sum(m["grants"] for m in measurements),
                    "refinements": sum(m["refinements"] for m in measurements),
                    "burned": sum(m["burned"] for m in measurements),
                    "presenceActions": sum(m["presenceActions"] for m in measurements),
                    "ordinalActions": sum(m["ordinalActions"] for m in measurements)})
        summary[name] = {"netPerStep": estimate([r["netPerStep"] for r in records]),
            "grossPerStep": estimate([r["gross"] / count for r in records]),
            "burnPerStep": estimate([r["burned"] / count for r in records]),
            "accuracyAnswered": total_correct / total_answered if total_answered else None,
            "answeredFraction": total_answered / (len(records) * count),
            "updates": sum(sum(row["learningUpdates"] for row in r["rows"]) for r in records),
            "refinements": sum(sum(row["refinements"] for row in r["rows"]) for r in records),
            "resets": sum(sum(row["resets"] for row in r["rows"]) for r in records),
            "comparisons": sum(sum(row["comparisons"] for row in r["rows"]) for r in records),
            "phaseSource": phases,
            "rareReturnFirst16": [phase_metrics(r, 3, 1, first=16) for r in records],
            "newUtilityLast64": [phase_metrics(r, 3, 3, last=64) for r in records]}
    contrast_specs = [
        ("useful communication", "opportunity/success-paid", "opportunity/success-paid/withdrawn"),
        ("interpretation learning package", "opportunity/success-paid", "no-interpretation-learning"),
        ("error gating", "error/upfront", "always/upfront"),
        ("opportunity gate alone", "opportunity/upfront", "matched/error/upfront"),
        ("success contingent payment", "opportunity/success-paid", "opportunity/upfront"),
        ("retain rare knowledge", "opportunity/success-paid", "opportunity/success-paid/use-reset"),
        ("ordinal renewal", "opportunity/success-paid", "opportunity/success-paid/no-renewal"),
        ("sixteen versus eight trial allowance", "opportunity/success-paid", "opportunity/success-paid/early-cutoff")]
    contrasts = {}
    for label, left, right in contrast_specs:
        deltas = [indexed[seed, left]["netPerStep"] - indexed[seed, right]["netPerStep"] for seed in seeds]
        contrasts[label] = {"left": left, "right": right, **estimate(deltas),
                            "positiveSeeds": sum(d > 0 for d in deltas)}
    analysis = {"protocol": completion, "settings": manifest["settings"], "arms": summary, "pairedContrasts": contrasts,
        "audit": {"allTaskAccountingChecks": True, "pairedTapes": True, "sourceHashesMatch": True,
                  "actualArchiveBehaviorChecks": len(archives), "totalTasks": len(runs) * count},
        "notes": ["Intervals use eight paired seed means; unadjusted Student t(7); descriptive, not multiplicity-corrected.",
                  "Per-source net assigns burns to their world step; not marginal causal utility.",
                  "Accuracy is among answered tasks; hold tasks remain in the primary group denominator.",
                  "Stage counts exclude edited steps because advice precedes feedback and landmark count follows it.",
                  "Costs are authored abstract resource prices, not measured CPU.",
                  "All supplied service scaffolds and learning rules are authored; no open-ended architecture invention."]}
    (directory / "analysis.json").write_text(json.dumps(analysis, indent=2))
    with (directory / "seed-metrics.csv").open("w", newline="") as output:
        writer = csv.writer(output); writer.writerow(["seed", "arm", "gross", "burned", "net_per_step"])
        for r in runs:
            writer.writerow([r["seed"], r["arm"], r["gross"], r["burned"], r["netPerStep"]])
    lines = ["# Fresh confirmation summary", "", "Eight paired seeds. Resource units per world task; costs abstract.", "",
             "| Arm | Net/task | 95% interval | Burn/task | Answered accuracy |", "|---|---:|---:|---:|---:|"]
    for name, value in summary.items():
        ci = value["netPerStep"]["ci95"]
        lines.append(f"| {name} | {value['netPerStep']['mean']:.3f} | [{ci[0]:.3f}, {ci[1]:.3f}] | {value['burnPerStep']['mean']:.3f} | {value['accuracyAnswered']:.1%} |")
    lines += ["", "## Paired changes", "", "| Contrast (left minus right) | Net/task change | 95% interval | Positive seeds |", "|---|---:|---:|---:|"]
    for name, value in contrasts.items():
        ci = value["ci95"]
        lines.append(f"| {name} | {value['mean']:.3f} | [{ci[0]:.3f}, {ci[1]:.3f}] | {value['positiveSeeds']}/8 |")
    lines += ["", "Intervals are unadjusted descriptive summaries of eight seed means.", ""]
    (directory / "SUMMARY.md").write_text("\n".join(lines))
    with zipfile.ZipFile(directory / "source-snapshot.zip", "w", zipfile.ZIP_DEFLATED) as archive:
        snapshot = list(manifest["sourceHashes"])
        for filename in ["Gojo.slnx", "AGENTS.md", "Directory.Build.props", "Directory.Build.targets", "global.json"]:
            if (repository / filename).is_file(): snapshot.append(filename)
        for folder in ["Gojo", "experiments/PrimitiveLearning", "experiments/CompositeEmergence", "experiments/AdaptiveLifecycle", "experiments/UtilityEcology"]:
            snapshot.extend(str(path.relative_to(repository)).replace("\\", "/") for path in (repository / folder).glob("*.csproj"))
        snapshot.extend("experiments/UtilityEcology/" + f for f in ["PLAN.md", "README.md", "REPORT.md", "Analyze.py", "Build-Review.py"])
        snapshot.extend(str(path.relative_to(repository)).replace("\\", "/") for path in
                        (repository / "experiments/UtilityEcology.Tests").glob("*.*") if path.suffix in [".cs", ".csproj"])
        for filename in sorted(set(snapshot)):
            archive.write(repository / filename, filename)
        for filename in ["manifest.json", "run.json", "archive-check.json"]:
            archive.write(directory / filename, "evidence/" + filename)
        archive.write(here / "verification/solution-summary.json", "evidence/solution-summary.json")
        if (here / "verification/dotnet-info.txt").is_file():
            archive.write(here / "verification/dotnet-info.txt", "evidence/dotnet-info.txt")
    print("\n".join(lines))


if __name__ == "__main__":
    main()
