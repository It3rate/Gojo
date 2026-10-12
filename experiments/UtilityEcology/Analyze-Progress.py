"""Analyze completed learning-progress runs without changing simulator inputs.

Standard library only. Raw JSON and native archives are read-only evidence;
analysis.json, SUMMARY.md and an optional source snapshot are derived artifacts.
Relative directory arguments are resolved beside this script.
"""
import argparse
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import statistics
import zipfile


HERE = Path(__file__).resolve().parent
REPOSITORY = HERE.parents[1]
ERROR = "progress/error-only/upfront"
SIGNED = "progress/signed/upfront"
CYCLE = "progress/cycle-only/upfront"
CHANGE = "progress/change/upfront"
ARM_NAMES = (ERROR, SIGNED, CYCLE, CHANGE)
BLOCK_SIZE = 16
MINIMUM_KNOWN = 8
COUNTERS = ("positiveEpisodes", "changeEpisodes", "coverageEpisodes",
            "retestEpisodes", "suppressedRequests")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def load_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def estimate(values, seed_level=True):
    values = list(values)
    if not values:
        return {"n": 0, "mean": None, "sd": None, "min": None, "max": None,
                "ci95": None, "values": []}
    mean = statistics.mean(values)
    sd = statistics.stdev(values) if len(values) > 1 else 0.0
    # Eight independent paired seeds: descriptive unadjusted Student t(7).
    # Development pilots with two seeds intentionally receive no interval.
    radius = 2.364624 * sd / math.sqrt(8) if seed_level and len(values) == 8 else None
    return {"n": len(values), "mean": mean, "sd": sd, "min": min(values),
            "max": max(values),
            "ci95": [mean - radius, mean + radius] if radius is not None else None,
            "values": values}


def safe_repository_file(filename):
    path = (REPOSITORY / filename).resolve()
    require(path.is_relative_to(REPOSITORY.resolve()), "Source path leaves repository: " + filename)
    return path


def load_and_audit(directory):
    completion = load_json(directory / "run.json")
    require(completion.get("status") == "complete", "Do not analyze an incomplete run")
    manifest = load_json(directory / "manifest.json")
    require(manifest.get("study") == "learning-progress", "Expected a learning-progress manifest")
    names = [arm["name"] for arm in manifest["arms"]]
    require(len(names) == 4 and set(names) == set(ARM_NAMES), "Expected the four declared progress arms")
    seeds = list(range(manifest["seedStart"], manifest["seedStart"] + manifest["seeds"]))
    count = manifest["settings"]["phaseLength"] * 4
    require(completion["seeds"] == len(seeds) and completion["arms"] == len(names),
            "Manifest and completion dimensions differ")
    require(completion["trials"] == len(seeds) * len(names) * count, "Completion task count differs")
    paths = sorted(p for p in directory.glob("*.json") if p.name.split("-", 1)[0].isdigit())
    runs = [load_json(path) for path in paths]
    indexed = {(r["seed"], r["arm"]): r for r in runs}
    expected = {(seed, name) for seed in seeds for name in names}
    require(len(runs) == len(indexed) and set(indexed) == expected, "Missing, duplicate or extra arm/seed run")
    initial = manifest["settings"]["initialCapital"]
    for run in runs:
        label = f"{run['seed']} / {run['arm']}"
        trace, rows = run["trace"], run["rows"]
        require(run["settings"] == manifest["settings"], "Settings differ: " + label)
        require(len(trace) == count and sum(r["steps"] for r in rows) == count, "Task count differs: " + label)
        require(sum(s["gross"] for s in trace) == sum(r["gross"] for r in rows) == run["gross"],
                "Gross totals differ: " + label)
        require(sum(s["burned"] for s in trace) == sum(r["burned"] for r in rows) == run["burned"],
                "Burn totals differ: " + label)
        require(abs((run["gross"] - run["burned"]) / count - run["netPerStep"]) < 1e-12,
                "Net arithmetic differs: " + label)
        require(run["accountedMoney"] == run["externalNet"] == initial + run["gross"],
                "Final currency differs: " + label)
        cumulative = 0
        for tick, step in enumerate(trace):
            require(step["tick"] == tick and step["phase"] == tick // manifest["settings"]["phaseLength"],
                    "Trace order or phase differs: " + label)
            require(step["seed"] == run["seed"] and step["arm"] == run["arm"], "Trace identity differs: " + label)
            require(step["source"] in range(4) and step["action"] in (-1, 0, 1), "Invalid actuator/source: " + label)
            cumulative += step["gross"]
            require(step["accountedMoney"] == step["externalNet"] == initial + cumulative,
                    f"Currency differs at {label}, tick {tick}")
            require(step["correct"] == (step["gross"] > 0), "Consequence indicator differs: " + label)
            require(step["action"] != 0 or step["gross"] == 0, "Hold has nonzero gross: " + label)
            require(step["burned"] >= 0, "Negative burned work: " + label)
        for row in rows:
            subset = [s for s in trace if s["phase"] == row["phase"] and s["source"] == row["source"]]
            for field, value in (
                    ("steps", len(subset)), ("gross", sum(s["gross"] for s in subset)),
                    ("burned", sum(s["burned"] for s in subset)),
                    ("correct", sum(s["correct"] for s in subset)),
                    ("answered", sum(s["action"] != 0 for s in subset)),
                    ("purchases", sum(s["bought"] for s in subset)),
                    ("interpreted", sum(s["interpreted"] for s in subset)),
                    ("refinements", sum(s["refined"] for s in subset)),
                    ("resets", sum(s["reset"] for s in subset))):
                require(row[field] == value, f"Row {field} differs: {label}")
    for seed in seeds:
        records = [indexed[seed, name] for name in names]
        require(len({r["tapeDigest"] for r in records}) == 1, f"Paired tape digests differ: {seed}")
        sequences = {tuple((s["phase"], s["source"]) for s in r["trace"]) for r in records}
        require(len(sequences) == 1, f"Paired external source schedules differ: {seed}")
    archives = load_json(directory / "archive-check.json")
    require(len(archives) == len(names), "Expected one first-seed native archive per arm")
    require({(a["seed"], a["arm"]) for a in archives} == {(seeds[0], name) for name in names},
            "Archive identities differ")
    archive_audit = []
    for entry in archives:
        require(all(entry[k] for k in ("signatureMatched", "monetaryMatched", "aliasesMatched")),
                "Native transport check failed")
        behavior = entry["behavior"]
        require(behavior["matched"] and behavior["continuedMoneyConserved"], "Native replay check failed")
        path = directory / entry["filename"]
        payload = path.read_bytes()
        require(len(payload) == entry["bytes"], "Archive length differs: " + entry["filename"])
        archive_audit.append({"filename": entry["filename"], "bytes": len(payload),
                              "sha256": digest(payload), "behaviorScope": behavior["scope"],
                              "samplesReplayed": behavior["samplesReplayed"],
                              "attentionSpent": behavior["attentionSpent"]})
    # An existing snapshot permits later analysis after the working tree changes.
    # No unchecked fallback or flag can waive a frozen source-hash mismatch.
    frozen_bytes = {}
    source_origin = {}
    snapshot_path = directory / "source-snapshot.zip"
    snapshot = zipfile.ZipFile(snapshot_path) if snapshot_path.is_file() else None
    try:
        for filename, expected_hash in manifest["sourceHashes"].items():
            live_path = safe_repository_file(filename)
            payload = live_path.read_bytes() if live_path.is_file() else None
            origin = "working-tree"
            if payload is None or digest(payload) != expected_hash:
                require(snapshot is not None and filename in snapshot.namelist(),
                        "Frozen source differs and no matching snapshot is available: " + filename)
                payload = snapshot.read(filename)
                origin = "frozen-snapshot"
            require(digest(payload) == expected_hash, "Frozen source hash differs: " + filename)
            frozen_bytes[filename] = payload
            source_origin[filename] = origin
    finally:
        if snapshot is not None:
            snapshot.close()
    return manifest, completion, indexed, seeds, names, archive_audit, frozen_bytes, source_origin


def inspect_progress(run):
    """Reconstruct evaluator window counts from prior public advice and outcomes.

    Decision snapshots precede this step's feedback; no private world labels
    are used. Native publications remain authoritative. Reconstruction is an
    audit/diagnostic projection, not input to the running gate.
    """
    events = {source: [] for source in range(4)}
    last_counters = {source: Counter() for source in range(4)}
    versions = {}
    facts = []
    for step in run["trace"]:
        source = step["source"]
        prior = events[source]
        p = step.get("progress")
        fact = {"step": step, "progress": p, "episodeDeltas": {}, "firstVersionRead": False,
                "windowKnown": None, "comparisonCompleted": False}
        if p is not None:
            require(p["observations"] == len(prior), f"Progress observation count differs at {run['seed']}, {step['tick']}")
            expected_version = max(0, len(prior) // BLOCK_SIZE - 1)
            require(p["version"] == expected_version, "Progress version differs from admitted event count")
            require(p["funded"] == step["learningGranted"], "Paid permission projection differs")
            completed_blocks = len(prior) // BLOCK_SIZE
            recent_losses = [event["loss"] for event in prior[(completed_blocks - 1) * BLOCK_SIZE:completed_blocks * BLOCK_SIZE]
                             if event["loss"] is not None] if completed_blocks else []
            expected_recent = sum(recent_losses) * 256 // len(recent_losses) if len(recent_losses) >= MINIMUM_KNOWN else None
            require(p["recentError256"] == expected_recent, "Native recent mean differs from captured pre-update errors")
            for counter in COUNTERS:
                delta = p[counter] - last_counters[source][counter]
                require(delta >= 0, "Progress counter decreased: " + counter)
                fact["episodeDeltas"][counter] = delta
                last_counters[source][counter] = p[counter]
            if p["version"] > 0:
                completed = len(prior) // BLOCK_SIZE
                old = prior[(completed - 2) * BLOCK_SIZE:(completed - 1) * BLOCK_SIZE]
                recent = prior[(completed - 1) * BLOCK_SIZE:completed * BLOCK_SIZE]
                old_known = [event["loss"] for event in old if event["loss"] is not None]
                new_known = [event["loss"] for event in recent if event["loss"] is not None]
                fact["windowKnown"] = {"older": len(old_known), "recent": len(new_known),
                                      "olderCoverage256": len(old_known) * 16,
                                      "recentCoverage256": len(new_known) * 16,
                                      "numericErrorSupported": min(len(old_known), len(new_known)) >= MINIMUM_KNOWN}
                require(p["coverageChange256"] == (len(new_known) - len(old_known)) * 16,
                        "Native coverage change differs from captured availability")
                require((p["errorProgress256"] is not None) == fact["windowKnown"]["numericErrorSupported"],
                        "Numeric error support differs from known window counts")
                expected_progress = sum(old_known) * 256 // len(old_known) - sum(new_known) * 256 // len(new_known) \
                    if fact["windowKnown"]["numericErrorSupported"] else None
                require(p["errorProgress256"] == expected_progress, "Native signed progress differs from captured pre-update errors")
                key = (source, p["version"])
                observed = (p["errorProgress256"], p["coverageChange256"])
                if key in versions:
                    require(versions[key]["values"] == observed, "Repeated comparison version changed its publication")
                else:
                    fact["firstVersionRead"] = True
                    versions[key] = {"source": source, "version": p["version"], "tick": step["tick"],
                                     "phase": step["phase"], "values": observed,
                                     "windowKnown": fact["windowKnown"]}
            else:
                require(p["errorProgress256"] is None and p["coverageChange256"] is None,
                        "Incomplete first comparison became numeric")
        # This matches this apparatus's genuine bought ±1 physical feedback.
        if step["bought"] and step["action"] in (-1, 1) and step["gross"] != 0:
            known = step["advice"] in (-1, 1)
            actual_direction = step["action"] * (1 if step["gross"] > 0 else -1)
            loss = (0 if step["advice"] == actual_direction else 2) if known else None
            prior.append({"loss": loss, "tick": step["tick"], "phase": step["phase"]})
            fact["comparisonCompleted"] = len(prior) >= 2 * BLOCK_SIZE and len(prior) % BLOCK_SIZE == 0
        facts.append(fact)
    run["_facts"] = facts
    run["_versions"] = list(versions.values())
    run["_finalObservationCounts"] = {str(k): len(v) for k, v in events.items()}


def progress_metrics(facts):
    facts = list(facts)
    decisions = [f for f in facts if f["progress"] is not None]
    first_reads = [f for f in decisions if f["firstVersionRead"]]
    triggers = {}
    for f in decisions:
        p = f["progress"]
        value = triggers.setdefault(p["trigger"], Counter())
        value["decisions"] += 1
        value["errorRequests"] += int(p["errorRequested"])
        value["gateRequests"] += int(p["gateRequested"])
        value["fundedGrants"] += int(p["funded"])
        value["currentPurchases"] += int(f["step"]["bought"])
    episodes = {k: sum(f["episodeDeltas"].get(k, 0) for f in facts) for k in COUNTERS}
    error_known = [f for f in first_reads if f["progress"]["errorProgress256"] is not None]
    return {
        "decisions": len(decisions),
        "errorRequests": sum(f["progress"]["errorRequested"] for f in decisions),
        "gateRequests": sum(f["progress"]["gateRequested"] for f in decisions),
        "fundedGrants": sum(f["progress"]["funded"] for f in decisions),
        "requestedNotFunded": sum(f["progress"]["gateRequested"] and not f["progress"]["funded"] for f in decisions),
        "errorRequestsSuppressed": sum(f["progress"]["errorRequested"] and not f["progress"]["gateRequested"] for f in decisions),
        "episodesOpened": episodes,
        "triggerCounts": {k: dict(v) for k, v in sorted(triggers.items())},
        "comparisonsCompleted": sum(f["comparisonCompleted"] for f in facts),
        "comparisonVersionsFirstRead": len(first_reads),
        "numericErrorVersions": len(error_known),
        "openErrorVersions": len(first_reads) - len(error_known),
        "positiveErrorVersions": sum(f["progress"]["errorProgress256"] > 0 for f in error_known),
        "regressingErrorVersions": sum(f["progress"]["errorProgress256"] < 0 for f in error_known),
        "zeroErrorVersions": sum(f["progress"]["errorProgress256"] == 0 for f in error_known),
        "positiveThresholdVersions": sum(f["progress"]["errorProgress256"] >= 128 for f in error_known),
        "regressionThresholdVersions": sum(f["progress"]["errorProgress256"] <= -128 for f in error_known),
        "coverageGainThresholdVersions": sum(f["progress"]["coverageChange256"] >= 128 for f in first_reads),
        "olderKnownPerFirstRead": estimate((f["windowKnown"]["older"] for f in first_reads), seed_level=False),
        "recentKnownPerFirstRead": estimate((f["windowKnown"]["recent"] for f in first_reads), seed_level=False),
        "lastEpisodeBudgetBySeedSource": {
            str(seed): {str(source): next((f["progress"]["episodeRemaining"] for f in reversed(decisions)
                                          if f["step"]["seed"] == seed and f["step"]["source"] == source), None)
                        for source in range(4)}
            for seed in sorted({f["step"]["seed"] for f in decisions})}
    }


def event_metrics(run, phase=None, source=None, first=None, purchased_only=False):
    facts = [f for f in run["_facts"] if (phase is None or f["step"]["phase"] == phase)
             and (source is None or f["step"]["source"] == source)]
    if purchased_only:
        facts = [f for f in facts if f["step"]["bought"]]
    if first is not None:
        facts = facts[:first]
    steps = [f["step"] for f in facts]
    n = len(steps)
    answered = sum(s["action"] != 0 for s in steps)
    bought = sum(s["bought"] for s in steps)
    correct = sum(s["correct"] for s in steps)
    gross, burn = sum(s["gross"] for s in steps), sum(s["burned"] for s in steps)
    return {"steps": n, "gross": gross, "burned": burn,
            "netPerStep": (gross - burn) / n if n else None,
            "grossPerStep": gross / n if n else None, "burnPerStep": burn / n if n else None,
            "answered": answered, "bought": bought, "correct": correct,
            "coverage": answered / n if n else None,
            "boughtFraction": bought / n if n else None,
            "accuracyAnswered": correct / answered if answered else None,
            "accuracyBought": sum(s["correct"] for s in steps if s["bought"]) / bought if bought else None,
            "interpreted": sum(s["interpreted"] for s in steps),
            "interpretationCoverage": sum(s["interpreted"] for s in steps) / n if n else None,
            "grants": sum(s["learningGranted"] for s in steps),
            "refinements": sum(s["refined"] for s in steps),
            "resets": sum(s["reset"] for s in steps),
            "progress": progress_metrics(facts)}


def aggregate(records, measurements):
    total_steps = sum(m["steps"] for m in measurements)
    total_answered = sum(m["answered"] for m in measurements)
    total_bought = sum(m["bought"] for m in measurements)
    total_correct = sum(m["correct"] for m in measurements)
    return {"netPerStep": estimate(m["netPerStep"] for m in measurements if m["netPerStep"] is not None),
            "grossPerStep": estimate(m["grossPerStep"] for m in measurements if m["grossPerStep"] is not None),
            "burnPerStep": estimate(m["burnPerStep"] for m in measurements if m["burnPerStep"] is not None),
            "coveragePerSeed": estimate(m["coverage"] for m in measurements if m["coverage"] is not None),
            "steps": total_steps, "answered": total_answered, "bought": total_bought,
            "correct": total_correct,
            "accuracyAnswered": total_correct / total_answered if total_answered else None,
            "accuracyBought": sum(m["accuracyBought"] * m["bought"] for m in measurements if m["accuracyBought"] is not None) / total_bought if total_bought else None,
            "coverage": total_answered / total_steps if total_steps else None,
            "boughtFraction": total_bought / total_steps if total_steps else None,
            "interpretationCoverage": sum(m["interpreted"] for m in measurements) / total_steps if total_steps else None,
            "grants": sum(m["grants"] for m in measurements),
            "refinements": sum(m["refinements"] for m in measurements),
            "resets": sum(m["resets"] for m in measurements),
            "perSeed": [{"seed": r["seed"], **m} for r, m in zip(records, measurements)]}


def selected_summary(records, predicate):
    measurements = []
    selected_facts = []
    for run in records:
        facts = [f for f in run["_facts"] if predicate(f["step"])]
        selected_facts.extend(facts)
        shadow = {**run, "_facts": facts}
        measurements.append(event_metrics(shadow))
    result = aggregate(records, measurements)
    rows = [row for r in records for row in r["rows"] if predicate(row)]
    result["updates"] = sum(row["learningUpdates"] for row in rows)
    result["comparisons"] = sum(row["comparisons"] for row in rows)
    result["progress"] = progress_metrics(selected_facts)
    return result


def common_adaptation(run):
    """Descriptive first error-free 16-request window after the common-law change."""
    steps = [s for s in run["trace"] if s["phase"] == 1 and s["source"] == 0]
    first = None
    for end in range(BLOCK_SIZE, len(steps) + 1):
        window = steps[end - BLOCK_SIZE:end]
        if all(s["action"] != 0 and s["correct"] for s in window):
            first = {"localRequestsThroughWindow": end,
                     "worldTasksSinceChange": window[-1]["tick"] - run["settings"]["phaseLength"] + 1,
                     "endingWorldTick": window[-1]["tick"]}
            break
    return {"seed": run["seed"], "criterion": "First 16 consecutive common-source requests all answered correctly in phase 1.",
            "firstWindow": first,
            "wholePhaseAccuracyAnswered": event_metrics(run, 1, 0)["accuracyAnswered"],
            "wholePhaseCoverage": event_metrics(run, 1, 0)["coverage"]}


def make_analysis(manifest, completion, indexed, seeds, names, archives, origins):
    arms = {}
    for name in names:
        records = [indexed[seed, name] for seed in seeds]
        group = selected_summary(records, lambda s: True)
        phases = []
        for phase in range(4):
            for source in range(4):
                metric = selected_summary(records, lambda s, p=phase, c=source: s["phase"] == p and s["source"] == c)
                if metric["steps"]:
                    phases.append({"phase": phase, "source": source, **metric})
        group["phaseSource"] = phases
        group["pureNoise"] = selected_summary(records, lambda s: s["source"] == 2)
        group["earlyOpportunityNoise"] = selected_summary(records, lambda s: s["source"] == 3 and s["phase"] < 3)
        group["newFinalUtility"] = selected_summary(records, lambda s: s["source"] == 3 and s["phase"] == 3)
        group["newFinalUtility"]["profitableSeeds"] = sum(
            r["gross"] > r["burned"] for r in group["newFinalUtility"]["perSeed"])
        group["rareReturnFirst16Requests"] = aggregate(
            records, [event_metrics(r, 3, 1, first=16) for r in records])
        group["rareReturnFirst16Purchases"] = aggregate(
            records, [event_metrics(r, 3, 1, first=16, purchased_only=True) for r in records])
        group["commonAdaptation"] = [common_adaptation(r) for r in records]
        group["observerFinalObservationCounts"] = [{"seed": r["seed"], "bySource": r["_finalObservationCounts"]} for r in records]
        arms[name] = group
    contrasts = {}
    for label, left, right, primary in (
            ("signed minus error", SIGNED, ERROR, True),
            ("signed minus cycle", SIGNED, CYCLE, True),
            ("change minus signed", CHANGE, SIGNED, False)):
        values = [indexed[seed, left]["netPerStep"] - indexed[seed, right]["netPerStep"] for seed in seeds]
        contrasts[label] = {"left": left, "right": right, "primary": primary, **estimate(values),
                            "positiveSeeds": sum(v > 0 for v in values)}
    return {
        "protocol": completion, "settings": manifest["settings"], "armDefinitions": manifest["arms"],
        "seeds": seeds, "arms": arms, "pairedContrasts": contrasts,
        "monitorLaw": {"blockSize": BLOCK_SIZE, "minimumKnownPerBlock": MINIMUM_KNOWN,
                       "equalityLossValues": [0, 2], "meanScale": 256,
                       "errorThreshold": 128, "coverageThreshold": 128,
                       "snapshotTiming": "Prior feedback and current funded decision, before current feedback."},
        "audit": {"allTaskAccountingChecks": True, "pairedTapesAndSourceSchedules": True,
                  "sourceHashesMatch": True, "sourceHashOrigins": origins,
                  "sourceHashes": manifest["sourceHashes"], "archives": archives,
                  "progressCountsAndCoverageChecks": True,
                  "totalTasks": completion["trials"]},
        "notes": [
            "Primary endpoint includes every world task, holds, startup and failed paid investigations.",
            "Eight seed estimates use descriptive unadjusted Student t(7) intervals; two-seed pilots have no confidence intervals.",
            "Internal upfront fees redistribute resources; physical gross minus burned work determines group return.",
            "Costs are abstract; passive monitor work is priced inside the existing one-unit inspection in all four arms.",
            "Phase/source descriptions are evaluator-only labels and never learner inputs.",
            "Pure noise means source 2 in all phases; source 3 is separately reported as early noise only in phases 0-2.",
            "Per-source burns are attributed to their world step, not marginal causal credit.",
            "Progress snapshots precede current feedback. Trigger and episode counters can record an episode opening without a funded current grant.",
            "Point progress counts deduplicate by seed, provider source and comparison version; adjacent versions can share a block.",
            "Evaluator window-known counts are reconstructed from prior bought public advice and actual consequences; they do not calibrate or train the monitor.",
            "Numeric progress is conditional on supported predictions; interpretation coverage and open comparisons remain separately visible.",
            "Common adaptation is a descriptive first error-free 16-request window in phase 1, not proof of permanent mastery or a primary endpoint.",
            "Observer comparisons completed at the last feedback may not have a later decision read; completed and first-read counts differ.",
            "Paid permissions, requested permissions and row-recorded actual model updates are different quantities.",
            "Saved-file checks cover their declared retained-anchor/presence and paid-decision fixture, not novel external world reconnection."
        ]}


def number(value, digits=3):
    return "—" if value is None else f"{value:.{digits}f}"


def percentage(value):
    return "—" if value is None else f"{value:.1%}"


def interval(value):
    return "—" if value["ci95"] is None else f"[{value['ci95'][0]:.3f}, {value['ci95'][1]:.3f}]"


def render_summary(analysis):
    n = len(analysis["seeds"])
    title = "Learning-progress confirmation" if n == 8 else "Learning-progress development run"
    lines = [f"# {title}", "", f"{n} paired seeds; {analysis['protocol']['totalTasks'] if 'totalTasks' in analysis['protocol'] else analysis['protocol']['trials']:,} total world tasks.",
             "Resource prices are abstract. The primary measure is physical gross minus burned work per **all** world tasks.", "",
             "## Group results", "",
             "| Arm | Net/task | 95% descriptive interval | Gross/task | Burn/task | Action coverage | Bought accuracy | Updates |",
             "|---|---:|---:|---:|---:|---:|---:|---:|"]
    for name, a in analysis["arms"].items():
        lines.append(f"| {name} | {number(a['netPerStep']['mean'])} | {interval(a['netPerStep'])} | {number(a['grossPerStep']['mean'])} | {number(a['burnPerStep']['mean'])} | {percentage(a['coverage'])} | {percentage(a['accuracyBought'])} | {a['updates']} |")
    lines += ["", "## Paired changes", "",
              "| Left minus right | Status | Net/task change | 95% descriptive interval | Positive seeds |",
              "|---|---|---:|---:|---:|"]
    for name, c in analysis["pairedContrasts"].items():
        lines.append(f"| {name} | {'Primary' if c['primary'] else 'Diagnostic'} | {number(c['mean'])} | {interval(c)} | {c['positiveSeeds']}/{n} |")
    lines += ["", ("Intervals use eight paired seed means and unadjusted Student t(7); they are descriptive, not simultaneous significance claims."
                   if n == 8 else "This development run has no confidence intervals. It is not fresh confirmation evidence."),
              "", "## Permission and funding", "",
              "| Arm | Decisions | Error requests | Final gate requests | Paid grants | Requested but unfunded | Suppressed error requests | Model updates |",
              "|---|---:|---:|---:|---:|---:|---:|---:|"]
    for name, a in analysis["arms"].items():
        p = a["progress"]
        lines.append(f"| {name} | {p['decisions']} | {p['errorRequests']} | {p['gateRequests']} | {p['fundedGrants']} | {p['requestedNotFunded']} | {p['errorRequestsSuppressed']} | {a['updates']} |")
    lines += ["", "## Unique progress comparisons and opened episodes", "",
              "| Arm | Versions first read | Numeric error versions | Open error versions | Positive threshold | Regression threshold | Coverage threshold | Positive episodes | Change episodes | Coverage episodes | Retests |",
              "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|"]
    for name, a in analysis["arms"].items():
        p, e = a["progress"], a["progress"]["episodesOpened"]
        lines.append(f"| {name} | {p['comparisonVersionsFirstRead']} | {p['numericErrorVersions']} | {p['openErrorVersions']} | {p['positiveThresholdVersions']} | {p['regressionThresholdVersions']} | {p['coverageGainThresholdVersions']} | {e['positiveEpisodes']} | {e['changeEpisodes']} | {e['coverageEpisodes']} | {e['retestEpisodes']} |")
    lines += ["", "Version counts are deduplicated per seed and provider. Adjacent versions may share one block.",
              "An episode opening is not necessarily a funded grant, and neither guarantees a useful model update.",
              "", "## Pure noise: source 2 in every phase", "",
              "| Arm | Net/task | Burn/task | Coverage | Paid grants | Updates | Refinements | Comparisons | Positive/change/coverage episodes |",
              "|---|---:|---:|---:|---:|---:|---:|---:|---|"]
    for name, a in analysis["arms"].items():
        m = a["pureNoise"]
        e = m["progress"]["episodesOpened"]
        lines.append(f"| {name} | {number(m['netPerStep']['mean'])} | {number(m['burnPerStep']['mean'])} | {percentage(m['coverage'])} | {m['progress']['fundedGrants']} | {m['updates']} | {m['refinements']} | {m['comparisons']} | {e['positiveEpisodes']}/{e['changeEpisodes']}/{e['coverageEpisodes']} |")
    lines += ["", "## Early noise and final new utility: source 3", "",
              "| Arm | Early-noise net/task | Early-noise paid grants | Final net/task | Profitable final seeds | Final coverage | Final bought accuracy | Numeric/open versions first read in final phase |",
              "|---|---:|---:|---:|---:|---:|---:|---|"]
    for name, a in analysis["arms"].items():
        early, final = a["earlyOpportunityNoise"], a["newFinalUtility"]
        p = final["progress"]
        lines.append(f"| {name} | {number(early['netPerStep']['mean'])} | {early['progress']['fundedGrants']} | {number(final['netPerStep']['mean'])} | {final['profitableSeeds']}/{n} | {percentage(final['coverage'])} | {percentage(final['accuracyBought'])} | {p['numericErrorVersions']}/{p['openErrorVersions']} |")
    lines += ["", "Final-phase versions are assigned to the phase of their first decision read; their blocks can include earlier-phase observations.",
              "A returning source's old comparison can be read in the new phase. Full per-seed window-known counts are in analysis.json.",
              "", "## Rare return: first 16 world requests in phase 3", "",
              "| Arm | Net/request | Coverage | Bought accuracy | Paid grants |",
              "|---|---:|---:|---:|---:|"]
    for name, a in analysis["arms"].items():
        m = a["rareReturnFirst16Requests"]
        lines.append(f"| {name} | {number(m['netPerStep']['mean'])} | {percentage(m['coverage'])} | {percentage(m['accuracyBought'])} | {m['grants']} |")
    lines += ["", "The first 16 **purchases** are also reported separately in analysis.json; these are different denominators.",
              "", "## Phase and source diagnostics", "",
              "| Arm | Phase (0-based) | Source | Net/task | Burn/task | Coverage | Bought accuracy | Updates | Refinements |",
              "|---|---:|---:|---:|---:|---:|---:|---:|---:|"]
    for name, a in analysis["arms"].items():
        for m in a["phaseSource"]:
            lines.append(f"| {name} | {m['phase']} | {m['source']} | {number(m['netPerStep']['mean'])} | {number(m['burnPerStep']['mean'])} | {percentage(m['coverage'])} | {percentage(m['accuracyBought'])} | {m['updates']} | {m['refinements']} |")
    lines += ["", "## Checks and limits", "",
              "Passed: per-step currency conservation; trace/row totals; paired tapes and source schedules; frozen source hashes; saved native transport, aliases and the declared behavior fixture; progress version/count/coverage consistency.",
              "", "The complete settings, arm definitions, per-seed variation, grant-trigger breakdowns, source hashes and archive digests are in analysis.json.", ""]
    lines.extend("- " + note for note in analysis["notes"])
    return "\n".join(lines) + "\n"


def write_snapshot(directory, manifest, frozen_bytes):
    """Freeze exact manifest sources and label supporting files separately."""
    files = dict(frozen_bytes)
    supporting = []
    for name in ("Gojo.slnx", "AGENTS.md", "Directory.Build.props", "Directory.Build.targets", "global.json"):
        if (REPOSITORY / name).is_file():
            supporting.append(name)
    for folder in ("Gojo", "Gojo.Tests", "experiments/PrimitiveLearning", "experiments/PrimitiveLearning.Tests",
                   "experiments/CompositeEmergence", "experiments/CompositeEmergence.Tests",
                   "experiments/AdaptiveLifecycle", "experiments/AdaptiveLifecycle.Tests",
                   "experiments/UtilityEcology", "experiments/UtilityEcology.Tests"):
        path = REPOSITORY / folder
        supporting.extend(p.relative_to(REPOSITORY).as_posix() for p in path.glob("*.csproj"))
        if folder.endswith(".Tests"):
            supporting.extend(p.relative_to(REPOSITORY).as_posix() for p in path.rglob("*.cs")
                              if not {"bin", "obj"}.intersection(p.relative_to(path).parts))
    supporting.extend(p.relative_to(REPOSITORY).as_posix()
                      for p in (REPOSITORY / "Gojo/docs").glob("*.md"))
    for name in ("PLAN.md", "PROGRESS-PLAN.md", "README.md", "REPORT.md", "PROGRESS-REPORT.md",
                 "Analyze.py", "Analyze-Progress.py", "Build-Review.py", "Build-ProgressReview.py"):
        if (HERE / name).is_file():
            supporting.append((HERE / name).relative_to(REPOSITORY).as_posix())
    for filename in sorted(set(supporting)):
        files.setdefault(filename, safe_repository_file(filename).read_bytes())
    records = {name: {"sha256": digest(payload),
                      "provenance": "manifest-frozen-source" if name in manifest["sourceHashes"] else "supporting-current-file"}
               for name, payload in sorted(files.items())}
    with zipfile.ZipFile(directory / "source-snapshot.zip", "w", zipfile.ZIP_DEFLATED) as archive:
        for filename, payload in sorted(files.items()):
            archive.writestr(filename, payload)
        for filename in ("manifest.json", "run.json", "archive-check.json"):
            archive.write(directory / filename, "evidence/" + filename)
        archive.writestr("SNAPSHOT.json", json.dumps({"files": records,
            "note": "Only manifest-frozen-source entries were hashed before the run; supporting files are packaged at analysis time."}, indent=2))
    return {"filename": "source-snapshot.zip", "files": records}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", nargs="?", default="results/progress-confirmation-01",
                        help="Completed run folder; relative paths resolve beside this script")
    parser.add_argument("--snapshot", action="store_true", help="Also package exact frozen sources and current supporting documents")
    args = parser.parse_args()
    directory = Path(args.directory)
    if not directory.is_absolute():
        directory = HERE / directory
    manifest, completion, indexed, seeds, names, archives, frozen_bytes, origins = load_and_audit(directory)
    for run in indexed.values():
        inspect_progress(run)
    analysis = make_analysis(manifest, completion, indexed, seeds, names, archives, origins)
    if args.snapshot:
        analysis["sourceSnapshot"] = write_snapshot(directory, manifest, frozen_bytes)
    # No derived output is written until all raw-evidence and source checks pass.
    (directory / "analysis.json").write_text(json.dumps(analysis, indent=2, allow_nan=False), encoding="utf-8")
    summary = render_summary(analysis)
    (directory / "SUMMARY.md").write_text(summary, encoding="utf-8")
    print(summary)


if __name__ == "__main__":
    main()
