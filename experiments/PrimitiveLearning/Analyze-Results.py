#!/usr/bin/env python3
"""Seed-level analysis of saved PrimitiveLearning trials; stdlib only.

Usage: python Analyze-Results.py artifacts/primitive-learning/campaign-fresh
       python Analyze-Results.py campaign-fresh persistent-fresh --output analysis-comparison
The script reads saved data; it never changes learners or schedules trials.
"""
from __future__ import annotations

import argparse
import itertools
import json
import math
from collections import defaultdict
from pathlib import Path
from statistics import mean, stdev


# Two-sided 95% Student t critical values by degrees of freedom. Campaign n8
# uses df7; persistent comparison n4 uses df3. No episode-level pseudo-replicates.
T95 = {1: 12.7062047364, 2: 4.3026527297, 3: 3.1824463053,
       4: 2.7764451052, 5: 2.5705818356, 6: 2.4469118488,
       7: 2.3646242510, 8: 2.3060041352, 9: 2.2621571629,
       10: 2.2281388520, 11: 2.2009851601, 12: 2.1788128297,
       13: 2.1603686565, 14: 2.1447866879, 15: 2.1314495456,
       16: 2.1199052992, 17: 2.1098155778, 18: 2.1009220402,
       19: 2.0930240544, 20: 2.0859634473, 21: 2.0796138447,
       22: 2.0738730679, 23: 2.0686576104, 24: 2.0638985616,
       25: 2.0595385528, 26: 2.0555294386, 27: 2.0518305165,
       28: 2.0484071418, 29: 2.0452296421, 30: 2.0422724563}
FULL = "A1P1Q1S1"
FACTORS = "APQS"
METRICS = ("returnPerEpisode", "correctRate", "coverage", "coveredAccuracy",
           "activeAccuracy", "stableCoverage", "stableAccuracy", "unstableCoverage", "unstableAccuracy",
           "queryRate", "stableQueryRate", "unstableQueryRate", "comparisonsPerEpisode", "auditArrivalsPerEpisode",
           "fitProbesPerEpisode", "shadowActionsPerEpisode", "landmarks", "predictorRows",
           "acceptedEdits", "selectedComplexity", "carriers", "relations", "placeholders", "coordinates")
PARITY_FIELDS = ("episodes", "returnPerEpisode", "correctRate", "coverage", "coveredAccuracy",
                 "activeAccuracy", "stableCoverage", "stableAccuracy", "unstableCoverage", "unstableAccuracy",
                 "queryRate", "stableQueryRate", "unstableQueryRate", "comparisons", "fitProbes",
                 "queries", "auditArrivals", "shadowActions", "landmarks", "predictorRows",
                 "acceptedEdits", "selectedComplexity", "graph", "decisionDigest", "learnedDigest")


def estimate(values):
    values = [float(v) for v in values if v is not None]
    if not values:
        return {"n": 0, "mean": None, "sd": None, "ci95": None}
    m = mean(values)
    if len(values) == 1:
        return {"n": 1, "mean": m, "sd": None, "ci95": None}
    df = len(values) - 1
    if df not in T95:
        raise ValueError(f"No exact stored t critical value for n={len(values)}; do not silently use a normal interval.")
    sd = stdev(values)
    half = T95[df] * sd / math.sqrt(len(values))
    return {"n": len(values), "mean": m, "sd": sd, "ci95": [m - half, m + half],
            "df": df, "tCritical": T95[df]}


def value(row, metric):
    conditional = {"coveredAccuracy": "coverage", "stableAccuracy": "stableCoverage", "unstableAccuracy": "unstableCoverage"}
    if metric in conditional and row.get(conditional[metric], 0) <= 0:
        return None
    if metric in ("carriers", "relations", "placeholders", "coordinates"):
        return row.get("graph", {}).get(metric)
    if metric.endswith("PerEpisode") and metric != "returnPerEpisode":
        field = metric[:-len("PerEpisode")]
        return row[field] / row["episodes"] if field in row and row["episodes"] else None
    return row.get(metric)


def summarize(rows):
    return {metric: estimate(value(row, metric) for row in rows) for metric in METRICS}


def load_json(path, default=None):
    return json.loads(path.read_text(encoding="utf-8-sig")) if path.exists() else default


def by_seed(rows):
    result = {}
    for row in rows:
        seed = row["seed"]
        if seed in result:
            raise ValueError(f"Duplicate seed {seed} within a matched result group.")
        result[seed] = row
    return result


def paired(left, right, label):
    a, b = by_seed(left), by_seed(right)
    seeds = sorted(a.keys() & b.keys())
    metrics = {}
    for metric in METRICS:
        deltas = {str(seed): value(a[seed], metric) - value(b[seed], metric) for seed in seeds
                  if value(a[seed], metric) is not None and value(b[seed], metric) is not None}
        metrics[metric] = {**estimate(deltas.values()), "seedDifferences": deltas}
    return {"label": label, "direction": "left minus right", "seeds": seeds,
            "unmatchedLeft": sorted(a.keys() - b.keys()), "unmatchedRight": sorted(b.keys() - a.keys()),
            "sameTape": all(a[s].get("tapeDigest") == b[s].get("tapeDigest") for s in seeds), "metrics": metrics}


def factorial(rows):
    variants = ["".join(f"{factor}{bit}" for factor, bit in zip(FACTORS, bits))
                for bits in itertools.product((0, 1), repeat=4)]
    cells = {variant: by_seed([r for r in rows if r["variant"] == variant]) for variant in variants}
    seeds = sorted(set.intersection(*(set(cell) for cell in cells.values())))
    contrasts = []
    # A k-factor interaction is an alternating sum of 2**k marginal cell means,
    # averaging equally over remaining factors. AS is the familiar difference
    # of differences. APSQ is its fourth-order analogue, not a main-effect scale.
    for factors in ["A", "P", "Q", "S", "AS", "APQS"]:
        metrics = {}
        for metric in METRICS:
            deltas = {}
            for seed in seeds:
                total = 0.0
                for variant in variants:
                    signs = {factor: 1 if variant[2 * i + 1] == "1" else -1 for i, factor in enumerate(FACTORS)}
                    v = value(cells[variant][seed], metric)
                    if v is None:
                        total = None
                        break
                    total += math.prod(signs[f] for f in factors) * v
                if total is not None:
                    deltas[str(seed)] = total * 2 ** len(factors) / 16
            metrics[metric] = {**estimate(deltas.values()), "seedContrasts": deltas}
        contrasts.append({"factors": factors, "order": len(factors), "metrics": metrics})
    return {"variantOrder": variants, "completeSeeds": seeds,
            "definition": "k-factor alternating sum of 2^k marginal cell means; remaining factors equally averaged. Main effects=on minus off; AS=difference of differences; APQS=fourth-order cell contrast.",
            "contrasts": contrasts, "cells": [{"variant": v, "metrics": summarize(cells[v].values())} for v in variants]}


def trace_summary(trace):
    result = {"ticks": len(trace), "scope": "Only the saved bounded full-controller trace (first120 test episodes), descriptive; not a substitute for full per-seed metrics.", "contexts": []}
    for context in sorted({step["context"] for step in trace}):
        selected = [step for step in trace if step["context"] == context]
        available = [step for step in selected if not step.get("missing") and not step.get("absent")]
        covered = [step for step in available if step.get("forecast") is not None]
        result["contexts"].append({"context": context, "ticks": len(selected), "available": len(available),
            "covered": len(covered), "correctCovered": sum(s["forecast"] == s["actual"] for s in covered),
            "queries": sum(bool(s.get("query")) for s in selected),
            "correctRoutes": sum(s.get("action") == (s["actual"] if s["fits"] else -s["actual"]) for s in available),
            "openFit": sum(s.get("fitRead") is None for s in selected),
            "openForecast": sum(s.get("forecast") is None for s in selected)})
    return result


def analyze(folder, allow_incomplete=False):
    manifest = load_json(folder / "manifest.json")
    rows = load_json(folder / "results.json")
    if not isinstance(manifest, dict) or not isinstance(rows, list):
        raise ValueError(f"{folder}: manifest.json and results.json are required.")
    settings = manifest.get("settings", {})
    expected = settings.get("seeds")
    seed_start = settings.get("seedStart", 0)
    completion = load_json(folder / "run.json")
    if completion is None and not allow_incomplete:
        raise ValueError(f"{folder}: no run.json completion record; use --allow-incomplete only for an explicitly preliminary inspection.")
    index = defaultdict(list)
    seen = set()
    for row in rows:
        key = (row["experiment"], row["variant"], row["condition"], row["phase"])
        unique = (*key, row["seed"])
        if unique in seen:
            raise ValueError(f"{folder}: duplicate result key {unique}.")
        seen.add(unique)
        index[key].append(row)
    warnings = []
    if completion is None:
        warnings.append("Run has no completion record; all findings are preliminary.")
    elif completion.get("rows") != len(rows):
        warnings.append(f"Completion row count {completion.get('rows')} differs from current results count {len(rows)}.")
    elif (completion.get("settings", {}).get("seeds") != expected or
          completion.get("settings", {}).get("seedStart", 0) != seed_start):
        warnings.append("Completion settings differ from the manifest's seed count/range.")
    for key, group in index.items():
        if expected is not None and sorted(r["seed"] for r in group) != list(range(seed_start, seed_start + expected)):
            warnings.append(f"Incomplete or unexpected seed group {key}: {[r['seed'] for r in group]}, expected {seed_start}..{seed_start+expected-1}.")
    if warnings and not allow_incomplete:
        raise ValueError(f"{folder}: " + " ".join(warnings))

    def get(experiment, variant, condition="baseline", phase="test"):
        return index.get((experiment, variant, condition, phase), [])

    tests = [r for r in rows if r["experiment"] == "composite" and r["condition"] == "baseline" and r["phase"] == "test"]
    full = get("composite", FULL)
    primary = []
    for control in ("A0P0Q0S0", "neutral-hold", "neutral-left", "neutral-alternating", "always-query", "A1P0Q1S1"):
        if get("composite", control):
            primary.append(paired(full, get("composite", control), f"{FULL} versus {control}"))
    secondary = []
    for control in ("A0P1Q1S1", "A1P1Q0S1", "A1P1Q1S0", "shuffled-feedback", "no-communication", "inverted-fit", "inverted-flow", "never-query", "whole-route-credit", "simultaneous-query"):
        if get("composite", control):
            secondary.append(paired(full, get("composite", control), f"{FULL} versus {control}"))
    locals_ = []
    for experiment in ("distinctions", "prediction", "queries", "programs"):
        variants = sorted({key[1] for key in index if key[0] == experiment and key[2] == "baseline" and key[3] == "test"})
        locals_.append({"experiment": experiment, "variants": [{"variant": v, "metrics": summarize(get(experiment, v))} for v in variants],
                       "pairedControls": [paired(get(experiment, "learned"), get(experiment, c), f"learned versus {c}")
                                          for c in variants if c != "learned"]})
    shifts = []
    for condition in sorted({row["condition"] for row in rows if row["variant"] == FULL and row["phase"] == "test"} - {"baseline", "encoding-1", "encoding-2"}):
        group = get("composite", FULL, condition)
        shifts.append({"condition": condition, "metrics": summarize(group),
                       "differenceFromBaseline": paired(group, full, f"{condition} minus baseline")})
    encodings = []
    base = by_seed(full)
    for condition in ("encoding-1", "encoding-2"):
        encoded = by_seed(get("composite", FULL, condition))
        checks = []
        for seed in sorted(base.keys() & encoded.keys()):
            a, b = base[seed], encoded[seed]
            differences = [field for field in PARITY_FIELDS if a.get(field) != b.get(field)]
            checks.append({"seed": seed, "matches": not differences, "differentFields": differences,
                           "sameTape": a.get("tapeDigest") == b.get("tapeDigest"),
                           "baselineDecisionDigest": a.get("decisionDigest"), "encodedDecisionDigest": b.get("decisionDigest")})
        encodings.append({"condition": condition, "checks": checks, "allMatch": bool(checks) and all(c["matches"] for c in checks),
                          "scope": "Actions/query decisions plus scalar outcomes/work counts/model footprint and learned native graph digest when supplied. Not cross-world carrier identity; private fixture encoding changes only."})
    curves = load_json(folder / "curves.json", [])
    curve_groups = defaultdict(list)
    for point in curves:
        curve_groups[(point["variant"], point["tick"])].append(point)
    curve_output = [{"variant": variant, "tick": tick,
                     "metrics": {m: estimate(p.get(m) for p in points) for m in ("returnPerEpisode", "correctRate", "landmarks", "predictorRows", "acceptedEdits")}}
                    for (variant, tick), points in sorted(curve_groups.items())]
    curve_changes = []
    for variant in sorted({point["variant"] for point in curves}):
        ticks = sorted({point["tick"] for point in curves if point["variant"] == variant})
        if len(ticks) > 1:
            first, last = by_seed(curve_groups[(variant, ticks[0])]), by_seed(curve_groups[(variant, ticks[-1])])
            seeds = sorted(first.keys() & last.keys())
            curve_changes.append({"variant": variant, "firstBlockEnds": ticks[0], "lastBlockEnds": ticks[-1],
                                  "seeds": seeds, "scope": "Paired last-minus-first training blocks, different physical episodes; descriptive learning progression, not a frozen-test causal contrast.",
                                  "metrics": {m: estimate(last[seed][m] - first[seed][m] for seed in seeds)
                                              for m in ("returnPerEpisode", "correctRate", "landmarks", "predictorRows", "acceptedEdits")}})
    baseline = manifest.get("baseline") or next((c for c in manifest.get("heldout", []) if c.get("name") == "baseline"), {})
    persistence = baseline.get("persistence", .95)
    fee = baseline.get("queryPrice", 1)
    stable_accuracy = max(persistence, 1 - persistence)
    stable_without_query = 4 * (2 * stable_accuracy - 1)
    exact_with_fee = 4 - fee
    ideal = {"assumptions": "Balanced contexts; known fit distinction; fixed relation law; query gives exact current direction; no missing/absence. These are model expectations, not measurements.",
             "predictableContextAccuracy": stable_accuracy, "unpredictableUnqueriedAccuracy": .5,
             "alwaysQueryReturn": exact_with_fee, "forecastNeverQueryReturn": stable_without_query / 2,
             "selectiveQueryReturn": (max(stable_without_query, exact_with_fee) + max(0, exact_with_fee)) / 2,
             "fixedPriorNeverQueryReturn": 2 * (2 * persistence - 1)}
    return {"path": str(folder.resolve()), "name": settings.get("name", folder.name), "manifest": manifest,
            "completion": completion, "warnings": warnings, "groups": [{"experiment": key[0], "variant": key[1], "condition": key[2], "phase": key[3], "metrics": summarize(group)} for key, group in sorted(index.items())],
            "primaryPairedComparisons": primary, "secondaryPairedComparisons": secondary,
            "factorial": factorial(tests), "localControls": locals_, "testShifts": shifts,
            "encodingParity": encodings, "learningCurves": curve_output, "learningCurveChanges": curve_changes,
            "boundedTrace": trace_summary(load_json(folder / "trace.json", [])),
            "idealReference": ideal, "archiveChecks": load_json(folder / "archive-check.json"),
            "archiveBytes": {path.name: path.stat().st_size for path in sorted(folder.glob("*.gojo"))}}


def fmt(stat, percentage=False, delta=False):
    if not stat or stat.get("mean") is None:
        return "unavailable"
    factor = 100 if percentage else 1
    unit = " pp" if percentage and delta else "%" if percentage else ""
    center = f"{stat['mean'] * factor:.2f}{unit}"
    interval = stat.get("ci95")
    if interval is None:
        return f"{center} (n={stat['n']}; no interval)"
    return f"{center} [{interval[0] * factor:.2f}, {interval[1] * factor:.2f}] (n={stat['n']})"


def markdown(studies):
    lines = ["# Primitive learning: saved-run analysis", "",
             "Intervals are two-sided 95% Student t intervals over independent seed means or paired seed differences. Episodes are not treated as independent replicates. Intervals describe this seed population; they are not corrected for multiple exploratory comparisons or a guarantee outside the authored task.", ""]
    for study in studies:
        settings = study["manifest"].get("settings", {})
        seed_start = settings.get("seedStart", 0)
        lines += [f"## {Path(study['path']).name}", "", f"Source: `{study['path']}`. {settings.get('seeds')} seeds starting at {seed_start}; {settings.get('train')} training and {settings.get('test')} test episodes per run.", ""]
        if study["warnings"]:
            lines += ["**Preliminary/incomplete:** " + " ".join(study["warnings"]), ""]
        lines += ["### Main test results", "", "| Variant | Return [95% CI] | Correct | Coverage | Covered accuracy | Query rate |", "|---|---:|---:|---:|---:|---:|"]
        for group in study["groups"]:
            if group["experiment"] == "composite" and group["phase"] == "test" and group["condition"] == "baseline":
                m = group["metrics"]
                lines.append(f"| {group['variant']} | {fmt(m['returnPerEpisode'])} | {fmt(m['correctRate'], True)} | {fmt(m['coverage'], True)} | {fmt(m['coveredAccuracy'], True)} | {fmt(m['queryRate'], True)} |")
        lines += ["", "### Prespecified paired primary contrasts", "", "Positive differences favor the full controller; each contrast uses matching seed tapes.", "", "| Full minus control | Return difference [95% CI] | Correct-rate difference | Query-rate difference |", "|---|---:|---:|---:|"]
        for contrast in study["primaryPairedComparisons"]:
            m = contrast["metrics"]
            lines.append(f"| {contrast['label']} | {fmt(m['returnPerEpisode'])} | {fmt(m['correctRate'], True, True)} | {fmt(m['queryRate'], True, True)} |")
        lines += ["", "### Auxiliary paired contrasts", "", "These controls change the declared information, feedback or training schedule. Effects apply to the specified consumer and retained initial policy.", "", "| Full minus control | Return difference [95% CI] | Correct-rate difference | Query-rate difference |", "|---|---:|---:|---:|---:|"]
        for contrast in study["secondaryPairedComparisons"]:
            m = contrast["metrics"]
            lines.append(f"| {contrast['label']} | {fmt(m['returnPerEpisode'])} | {fmt(m['correctRate'], True, True)} | {fmt(m['queryRate'], True, True)} |")
        lines += ["", "### Factorial contrasts", "", study["factorial"]["definition"], "", "| Contrast | Return [95% CI] | Correct-rate contrast |", "|---|---:|---:|"]
        for contrast in study["factorial"]["contrasts"]:
            lines.append(f"| {contrast['factors']} | {fmt(contrast['metrics']['returnPerEpisode'])} | {fmt(contrast['metrics']['correctRate'], True, True)} |")
        lines += ["", "### Isolated primitives", "", "| Primitive / control | Correct | Coverage | Accuracy when covered | Return |", "|---|---:|---:|---:|---:|"]
        for local in study["localControls"]:
            for group in local["variants"]:
                m = group["metrics"]
                lines.append(f"| {local['experiment']} / {group['variant']} | {fmt(m['correctRate'], True)} | {fmt(m['coverage'], True)} | {fmt(m['coveredAccuracy'], True)} | {fmt(m['returnPerEpisode'])} |")
        prediction = next((g for g in study["groups"] if g["experiment"] == "prediction" and g["variant"] == "learned" and g["condition"] == "baseline" and g["phase"] == "test"), None)
        if prediction and prediction["metrics"]["stableCoverage"]["n"]:
            m = prediction["metrics"]
            lines += ["", f"Isolated learned predictor: predictable cue0 coverage {fmt(m['stableCoverage'], True)}, accuracy when covered {fmt(m['stableAccuracy'], True)}; unreliable cue1 coverage {fmt(m['unstableCoverage'], True)}, accuracy when covered {fmt(m['unstableAccuracy'], True)}. Cue0 denotes a predictable relationship, which can be reversal or repetition; it does not imply physical persistence."]
        lines += ["", "### Frozen test shifts", "", "| Condition | Return [95% CI] | Change from baseline | Correct | Query rate |", "|---|---:|---:|---:|---:|"]
        for shift in study["testShifts"]:
            m = shift["metrics"]
            lines.append(f"| {shift['condition']} | {fmt(m['returnPerEpisode'])} | {fmt(shift['differenceFromBaseline']['metrics']['returnPerEpisode'])} | {fmt(m['correctRate'], True)} | {fmt(m['queryRate'], True)} |")
        full = next((g for g in study["groups"] if g["experiment"] == "composite" and g["variant"] == FULL and g["condition"] == "baseline" and g["phase"] == "test"), None)
        if full:
            m = full["metrics"]
            lines += ["", "### Query placement, work and retained graph", "", f"Full query rate in the predictable cue0 context: {fmt(m['stableQueryRate'], True)}; unreliable cue1 context: {fmt(m['unstableQueryRate'], True)}.", "",
                      f"Deployment comparisons/episode: {fmt(m['comparisonsPerEpisode'])}; fit probes/episode: {fmt(m['fitProbesPerEpisode'])}; shadow actions/episode: {fmt(m['shadowActionsPerEpisode'])}.", "",
                      f"Full graph: carriers {fmt(m['carriers'])}; relations {fmt(m['relations'])}; placeholders {fmt(m['placeholders'])}; coordinate occurrences {fmt(m['coordinates'])}. Active landmarks {fmt(m['landmarks'])}; predictor rows {fmt(m['predictorRows'])}; accepted edits {fmt(m['acceptedEdits'])}; selected complexity {fmt(m['selectedComplexity'])}."]
        if study["archiveBytes"]:
            lines += ["", f"Seed {seed_start} native archive sizes (transport bytes, not process memory): " + "; ".join(f"{name}: {size:,}" for name, size in study["archiveBytes"].items()) + "."]
        lines += ["", "### Private encoding checks", ""]
        for encoding in study["encodingParity"]:
            differences = [(c["seed"], c["differentFields"]) for c in encoding["checks"] if not c["matches"]]
            lines.append(f"- {encoding['condition']}: {len(encoding['checks'])} paired seeds; all decisions/outcomes/work/footprint equal: **{encoding['allMatch']}**. Differences: `{differences}`.")
        ideal = study["idealReference"]
        lines += ["", "### Ideal reference and scope", "", ideal["assumptions"], "",
                  f"Expected selective-query return {ideal['selectiveQueryReturn']:.3f}; always-query {ideal['alwaysQueryReturn']:.3f}; learned forecast without queries {ideal['forecastNeverQueryReturn']:.3f}; fixed previous-direction without queries {ideal['fixedPriorNeverQueryReturn']:.3f}.", "",
                  "The complete seed curves and auxiliary paired contrasts are in analysis.json. Curves are training block outcomes, not frozen test performance. The saved trace is a small descriptive window, not another independent replicate.", ""]
    lines += ["## Limits shared by these runs", "",
              "- All 16 program grammar candidates were constructed before the campaign. This evaluates bounded native selection and local composition, not unrestricted program discovery.",
              "- Adaptive distinctions are piecewise-constant consequence hypotheses. Predictor count alternatives are empirical observations, not guaranteed sensor support.",
              "- Query training uses a declared known-fitting shadow task. Shadow work is counted; comparison CPU work and training audits are not debited as the deployment resource fee.",
              "- Frozen tests check fixed learned models. A changed law or price is not adapted during those tests, so degradation measures transfer of that fixed policy.",
              "- Native graph sizes are retained reachable structures, including bounded receipts and evidence. They are neither process memory nor the number of active learned rules alone.",
              "- Conditional accuracy is averaged over seeds with nonzero coverage and accompanies its own n. Zero-coverage seeds are excluded from that statistic rather than treated as 0% accurate; paired accuracy contrasts require coverage on both sides.",
              "- Student t intervals are not clipped to 0–100%; small seed samples can produce wide intervals beyond physical rate bounds.",
              "- Neutral/ablated models may have limited information or initial grammar. Measured complementarity does not prove every remaining algorithm incapable.", ""]
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folders", nargs="+", type=Path)
    parser.add_argument("--output", type=Path, help="Analysis output directory (defaults to first folder).")
    parser.add_argument("--allow-incomplete", action="store_true")
    args = parser.parse_args()
    studies = [analyze(folder, args.allow_incomplete) for folder in args.folders]
    output = args.output or args.folders[0]
    output.mkdir(parents=True, exist_ok=True)
    document = {"method": "Seed-paired means; two-sided Student t95 intervals with seed as independent replicate; no episode pseudoreplication.",
                "studies": studies, "crossStudyComparisons": [], "sourceHashComparisons": []}
    if len(studies) > 1:
        # These compare protocols descriptively, not additional untouched tests.
        raw = [load_json(folder / "results.json") for folder in args.folders]
        for i, j in itertools.combinations(range(len(studies)), 2):
            hashes_a = studies[i]["manifest"].get("sourceHashes")
            hashes_b = studies[j]["manifest"].get("sourceHashes")
            document["sourceHashComparisons"].append({"left": studies[i]["path"], "right": studies[j]["path"],
                "available": isinstance(hashes_a, dict) and isinstance(hashes_b, dict),
                "allMatch": isinstance(hashes_a, dict) and isinstance(hashes_b, dict) and hashes_a == hashes_b})
            for variant in (FULL, "A1P0Q1S1", "always-query", "never-query"):
                a = [r for r in raw[i] if r["experiment"] == "composite" and r["variant"] == variant and r["condition"] == "baseline" and r["phase"] == "test"]
                b = [r for r in raw[j] if r["experiment"] == "composite" and r["variant"] == variant and r["condition"] == "baseline" and r["phase"] == "test"]
                if a and b:
                    contrast = paired(a, b, f"{Path(studies[i]['path']).name} minus {Path(studies[j]['path']).name}: {variant}")
                    contrast["scope"] = "Descriptive paired protocol comparison on shared seed identities, not additional independent replications. Same seed indexes do not imply identical physical tapes or laws; settings and completion status remain explicit in each manifest."
                    document["crossStudyComparisons"].append(contrast)
    (output / "analysis.json").write_text(json.dumps(document, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    (output / "analysis.md").write_text(markdown(studies), encoding="utf-8")
    print(json.dumps({"output": str(output.resolve()), "studies": len(studies),
                      "complete": all(not study["warnings"] for study in studies),
                      "encodingParity": [{"path": study["path"], "checks": [{"condition": e["condition"], "allMatch": e["allMatch"]} for e in study["encodingParity"]]} for study in studies]}))


if __name__ == "__main__":
    main()
