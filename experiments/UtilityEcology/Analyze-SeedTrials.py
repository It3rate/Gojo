"""Audit and summarize a completed opt-in protected-candidate study.

Standard library only. Analysis never feeds the native controller or changes a
study parameter. Relative input paths resolve beside this script.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import statistics
import tempfile
import zipfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
ERROR, PROGRESS = "seed/error-only", "seed/progress"
PAIRED, UNFILTERED, NOSEED = "seed/paired", "seed/unfiltered", "seed/no-seed"
NAMES = (ERROR, PROGRESS, PAIRED, UNFILTERED, NOSEED)
LABELS = {ERROR: "Existing error", PROGRESS: "Existing progress", PAIRED: "Seed + paired",
          UNFILTERED: "Seed + unfiltered", NOSEED: "Paired, no seed"}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(payload):
    return hashlib.sha256(payload).hexdigest()


def estimate(values):
    values = list(values)
    n = len(values)
    mean = statistics.mean(values) if n else None
    interval = None
    if n == 8:
        margin = 2.364624251 * statistics.stdev(values) / n ** .5
        interval = [mean - margin, mean + margin]
    return {"n": n, "mean": mean, "ci95": interval, "values": values}


def repository_path(name):
    path = (ROOT / name).resolve()
    require(path.is_relative_to(ROOT.resolve()), "Source path escapes repository: " + name)
    return path


def load(directory):
    completion, manifest = read(directory / "run.json"), read(directory / "manifest.json")
    require(completion.get("status") == "complete", "Do not analyze an incomplete run")
    require(manifest.get("study") == "protected-seed-trials", "Expected protected-seed-trials")
    names = [a["name"] for a in manifest["arms"]]
    require(len(names) == 5 and set(names) == set(NAMES), "Expected the five declared arms")
    seeds = list(range(manifest["seedStart"], manifest["seedStart"] + manifest["seeds"]))
    count = 4 * manifest["settings"]["phaseLength"]
    require(completion["seeds"] == len(seeds) and completion["arms"] == 5 and
            completion["trials"] == len(seeds) * 5 * count, "Completion dimensions differ")
    paths = sorted(p for p in directory.glob("*.json") if p.name.split("-", 1)[0].isdigit())
    runs = [read(p) for p in paths]
    indexed = {(r["seed"], r["arm"]): r for r in runs}
    require(len(runs) == len(indexed) and set(indexed) == {(s, a) for s in seeds for a in names},
            "Missing, duplicate or extra arm/seed run")
    initial = manifest["settings"]["initialCapital"]
    for run in runs:
        ecology = run["ecology"]
        label = f"{run['seed']} / {run['arm']}"
        require((ecology["seed"], ecology["arm"]) == (run["seed"], run["arm"]), "Nested identity differs: " + label)
        require(ecology["settings"] == manifest["settings"], "Settings differ: " + label)
        trace, rows = ecology["trace"], ecology["rows"]
        require(len(trace) == count and sum(r["steps"] for r in rows) == count, "Task count differs: " + label)
        for field in ("gross", "burned"):
            require(sum(s[field] for s in trace) == sum(r[field] for r in rows) == ecology[field],
                    "Totals differ for " + field + ": " + label)
        require(abs(ecology["netPerStep"] - (ecology["gross"] - ecology["burned"]) / count) < 1e-12,
                "Net arithmetic differs: " + label)
        cumulative = 0
        for tick, step in enumerate(trace):
            require(step["tick"] == tick and step["phase"] == tick // manifest["settings"]["phaseLength"],
                    "Trace coordinate differs: " + label)
            require((step["seed"], step["arm"]) == (run["seed"], run["arm"]), "Trace identity differs: " + label)
            require(step["source"] in range(4) and step["action"] in (-1, 0, 1), "Invalid source/action: " + label)
            require(step["correct"] == (step["gross"] > 0), "Outcome indicator differs: " + label)
            require(step["gross"] in ((-24, 0, 16) if step["source"] == 1 else (-12, 0, 8)),
                    "Physical payout differs: " + label)
            require((step["action"] == 0) == (step["gross"] == 0), "Hold consequence differs: " + label)
            require(step["burned"] >= 0, "Negative burn: " + label)
            require(step["advice"] in (None, -1, 1) and step["interpreted"] == (step["advice"] is not None),
                    "Open advice projection differs: " + label)
            cumulative += step["gross"]
            require(step["accountedMoney"] == step["externalNet"] == initial + cumulative,
                    f"Currency differs at {label}, tick {tick}")
        require(ecology["accountedMoney"] == ecology["externalNet"] == initial + cumulative,
                "Final currency differs: " + label)
        require(len({(r["phase"], r["source"]) for r in rows}) == len(rows), "Duplicate aggregate rows")
        for row in rows:
            subset = [s for s in trace if (s["phase"], s["source"]) == (row["phase"], row["source"])]
            values = {"steps": len(subset), "gross": sum(s["gross"] for s in subset),
                      "burned": sum(s["burned"] for s in subset), "correct": sum(s["correct"] for s in subset),
                      "answered": sum(s["action"] != 0 for s in subset), "purchases": sum(s["bought"] for s in subset),
                      "interpreted": sum(s["interpreted"] for s in subset), "refinements": sum(s["refined"] for s in subset),
                      "resets": sum(s["reset"] for s in subset)}
            require(all(row[k] == v for k, v in values.items()), "Aggregate row differs: " + label)
            require(abs(row["netPerStep"] - (row["gross"] - row["burned"]) / row["steps"]) < 1e-12,
                    "Aggregate net differs: " + label)
        inspect_trials(run, manifest)
    for seed in seeds:
        group = [indexed[seed, name]["ecology"] for name in names]
        require(len({r["tapeDigest"] for r in group}) == 1, "Paired tapes differ")
        require(len({tuple((s["phase"], s["source"]) for s in r["trace"]) for r in group}) == 1,
                "Paired external request schedules differ")
    checks = read(directory / "archive-check.json")
    require({(c["seed"], c["arm"]) for c in checks} == {(seeds[0], n) for n in names} and len(checks) == 5,
            "Expected one first-seed archive per arm")
    archives = []
    for check in checks:
        behavior = check["behavior"]
        require(check["signatureMatched"] and behavior["matched"] and behavior["conserved"] and behavior["aliasesMatched"],
                "Native archive check failed: " + check["arm"])
        closed = sum(e["promoted"] or e["refused"] for e in indexed[seeds[0], check["arm"]]["_trialEvents"])
        require(behavior.get("trialDecisionsReplayed", 0) == closed, "Scoped archive admission replay count differs")
        archive = (directory / check["filename"]).resolve()
        require(archive.is_relative_to(directory.resolve()), "Archive path escapes run directory")
        payload = archive.read_bytes()
        require(len(payload) == check["bytes"], "Archive byte count differs")
        archives.append({**check, "sha256": digest(payload)})
    frozen, origins = {}, {}
    snapshot_path = directory / "source-snapshot.zip"
    snapshot = zipfile.ZipFile(snapshot_path) if snapshot_path.is_file() else None
    try:
        freeze_path = directory / "SNAPSHOT.json"
        if freeze_path.is_file():
            freeze = read(freeze_path)
            require(freeze["allHashesMatch"] and freeze["files"] == len(manifest["sourceHashes"]),
                    "Pre-run source freeze record differs")
            if "protocolHash" in freeze:
                require(snapshot is not None and "protocol-at-freeze.md" in snapshot.namelist() and
                        digest(snapshot.read("protocol-at-freeze.md")) == freeze["protocolHash"],
                        "Frozen protocol differs from pre-run record")
            manifest["_freezeAudit"] = freeze
        for filename, expected in manifest["sourceHashes"].items():
            path = repository_path(filename)
            payload = path.read_bytes() if path.is_file() else None
            origin = "working-tree"
            if payload is None or digest(payload) != expected:
                require(snapshot is not None and filename in snapshot.namelist(),
                        "Frozen source differs and no matching snapshot exists: " + filename)
                payload, origin = snapshot.read(filename), "frozen-snapshot"
            require(digest(payload) == expected, "Frozen source hash differs: " + filename)
            frozen[filename], origins[filename] = payload, origin
    finally:
        if snapshot is not None:
            snapshot.close()
    return manifest, completion, indexed, seeds, names, archives, frozen, origins


def inspect_trials(run, manifest):
    trace, states = run["ecology"]["trace"], run["seedTrace"]
    arm = next(a for a in manifest["arms"] if a["name"] == run["arm"])
    require(bool(states) == arm["protectedTrials"], "Protected trace presence differs")
    run["_trialEvents"], run["_statesBySource"], run["_closedAt"] = [], {}, {}
    if not states:
        return
    require(len(states) == len(trace), "Trial and physical traces are not aligned")
    previous = {s: {"trials": 0, "accepted": 0, "rejected": 0, "seedSpent": 0, "earnedSpent": 0} for s in range(4)}
    prior_trial = {s: {"lastTrialId": 0, "lastGain": 0, "lastCoverage": 0, "validationCount": 0} for s in range(4)}
    purchases = Counter()
    launches = {}
    for step, state in zip(trace, states):
        source, tick = step["source"], step["tick"]
        require((state["tick"], state["source"]) == (tick, source), "Trial trace coordinate differs")
        old = previous[source]
        deltas = {k: state[k] - old[k] for k in old}
        require(all(v >= 0 for v in deltas.values()), "Local trial counter decreased")
        require(deltas["trials"] == int(state["launched"]) and deltas["accepted"] == int(state["promoted"]) and
                deltas["rejected"] == int(state["refused"]), "Trial event/counter projection differs")
        require(state["accepted"] + state["rejected"] <= state["trials"] <= manifest["maximumAttempts"],
                "Trial completion count or limit differs")
        require(state["trialWork"] == state["seedSpent"] + state["earnedSpent"], "Trial work sum differs")
        require(0 <= state["seedSpent"] <= arm["seedEndowment"] and
                state["seedRemaining"] == arm["seedEndowment"] - state["seedSpent"], "Seed conservation differs")
        require(deltas["seedSpent"] + deltas["earnedSpent"] <= step["burned"], "Trial costs exceed actual burn")
        require(not (old["accepted"] + old["rejected"] > 0 and deltas["seedSpent"]), "Seed subsidized a later trial")
        require(state["trainingCount"] <= manifest["trainingEvents"] and state["validationCount"] <= manifest["validationEvents"],
                "Trial event capacity exceeded")
        if step["bought"]:
            purchases[source] += 1
        if state["launched"]:
            if source in launches:
                require(purchases[source] - launches[source] >= manifest["retryPeriod"], "Retry spacing differs")
            launches[source] = purchases[source]
        event = {"seed": run["seed"], "arm": run["arm"], "tick": tick, "phase": step["phase"], "source": source,
                 "seedWorkDelta": deltas["seedSpent"], "earnedWorkDelta": deltas["earnedSpent"],
                 "launched": state["launched"], "promoted": state["promoted"], "refused": state["refused"]}
        prior = prior_trial[source]
        same_trial = prior["lastTrialId"] == state["lastTrialId"]
        advanced = state["validationCount"] - prior["validationCount"] if same_trial else state["validationCount"]
        require(advanced in (0, 1), "Validation advanced by more than one physical event")
        if advanced:
            require(state["validationPrepared"] and step["bought"] and step["action"] in (-1, 1) and step["gross"] != 0,
                    "Validation lacks paid pre-feedback preparation or valid physical outcome")
            incumbent, candidate = state["incumbentValidationAdvice"], state["candidateValidationAdvice"]
            require(incumbent in (None, -1, 1) and candidate in (None, -1, 1), "Invalid paired publication")
            increment = 0 if candidate is None else projected_gross(candidate, step["action"], step["gross"]) - projected_gross(incumbent, step["action"], step["gross"])
            require(state["lastGain"] == prior["lastGain"] + increment and
                    state["lastCoverage"] == prior["lastCoverage"] + int(candidate is not None),
                    "Native paired gain or supported count differs from pre-feedback publications")
            event["validationGainDelta"] = increment
        elif same_trial:
            require(state["lastGain"] == prior["lastGain"] and state["lastCoverage"] == prior["lastCoverage"],
                    "Gain or coverage changed without an admitted validation event")
        else:
            require(state["lastGain"] == 0 and state["lastCoverage"] == 0, "New candidate inherited validation gain")
        if state["promoted"] or state["refused"]:
            require(all(k in state for k in ("lastTrialId", "lastGain", "lastCoverage", "lastTrialBurn")),
                    "Closed trial needs gain, coverage, work and identity diagnostics")
            complete = state["validationCount"] == manifest["validationEvents"]
            event.update({"id": state["lastTrialId"], "gain": state["lastGain"], "coverage": state["lastCoverage"],
                          "burn": state["lastTrialBurn"], "netGain": state["lastGain"] - state["lastTrialBurn"],
                          "completed": complete,
                          "reason": state.get("lastTrialReason") or ("incomplete" if not complete else
                              "coverage" if state["lastCoverage"] < manifest["minimumKnownValidation"] else
                              "nonpositive-net-gain" if state["lastGain"] <= state["lastTrialBurn"] else "positive-net-gain")})
            require(state["lastTrialId"] == state["trials"], "Closing trial identity differs")
            require(0 <= event["coverage"] <= state["validationCount"], "Trial known coverage differs")
            require(0 <= event["burn"] <= manifest["trialWorkCap"], "Trial work capacity exceeded")
            if state["promoted"]:
                require(complete, "Incomplete candidate was promoted")
                if arm["compareAdmission"]:
                    require(event["coverage"] >= manifest["minimumKnownValidation"] and event["netGain"] > 0,
                            "Paired admission violates native criterion")
                following = [s for s in trace[tick + 1:] if s["source"] == source and s["bought"]]
                stop = next((s["tick"] for s in states[tick + 1:] if s["source"] == source and s["promoted"]), None)
                following = [s for s in following if stop is None or s["tick"] < stop]
                event["fresh16"] = metrics(following[:16])
                event["fresh16"]["windowComplete"] = len(following) >= 16
            if source not in run["_closedAt"]:
                run["_closedAt"][source] = tick
        run["_trialEvents"].append(event)
        previous[source] = {k: state[k] for k in old}
        prior_trial[source] = {k: state[k] for k in prior}
        run["_statesBySource"][source] = state


def projected_gross(advice, action, gross):
    """Audit the supplied native opposed-action payout correspondence.

    This uses the actual issued action and its physical consequence, not a
    hidden desired-action label. It never becomes money or learner evidence.
    """
    if advice is None or advice == action:
        return gross
    numerator, denominator = (-3 * gross, 2) if gross > 0 else (-2 * gross, 3)
    require(numerator % denominator == 0, "Declared physical payoff does not settle exactly")
    return numerator // denominator


def metrics(steps):
    steps = list(steps)
    n = len(steps)
    gross, burn = sum(s["gross"] for s in steps), sum(s["burned"] for s in steps)
    answered = sum(s["action"] != 0 for s in steps)
    bought, correct = sum(s["bought"] for s in steps), sum(s["correct"] for s in steps)
    return {"steps": n, "gross": gross, "burned": burn, "netPerStep": (gross - burn) / n if n else None,
            "grossPerStep": gross / n if n else None, "burnPerStep": burn / n if n else None,
            "answered": answered, "bought": bought, "correct": correct,
            "coverage": answered / n if n else None, "accuracyAnswered": correct / answered if answered else None,
            "accuracyBought": sum(s["correct"] for s in steps if s["bought"]) / bought if bought else None,
            "interpreted": sum(s["interpreted"] for s in steps), "grants": sum(s["learningGranted"] for s in steps)}


def aggregate(runs, selector):
    values = [{"seed": r["seed"], **metrics(selector(r))} for r in runs]
    totals = {k: sum(v[k] for v in values) for k in ("steps", "gross", "burned", "answered", "bought", "correct", "interpreted", "grants")}
    return {**totals, **{k: estimate(v[k] for v in values if v[k] is not None) for k in
                        ("netPerStep", "grossPerStep", "burnPerStep")},
            "coverage": totals["answered"] / totals["steps"] if totals["steps"] else None,
            "interpretationCoverage": totals["interpreted"] / totals["steps"] if totals["steps"] else None,
            "accuracyAnswered": totals["correct"] / totals["answered"] if totals["answered"] else None,
            "accuracyBought": sum(v["accuracyBought"] * v["bought"] for v in values if v["accuracyBought"] is not None) / totals["bought"] if totals["bought"] else None,
            "perSeed": values}


def trial_metrics(runs, predicate=lambda e: True):
    events = [e for r in runs for e in r["_trialEvents"] if predicate(e)]
    closures = [e for e in events if e["promoted"] or e["refused"]]
    return {"launches": sum(e["launched"] for e in events), "promotions": sum(e["promoted"] for e in events),
            "rejections": sum(e["refused"] for e in events), "completions": sum(e["completed"] for e in closures),
            "aborts": sum(not e["completed"] for e in closures),
            "coverageFailures": sum(e["completed"] and e["coverage"] < 24 for e in closures),
            "seedWork": sum(e["seedWorkDelta"] for e in events), "earnedWork": sum(e["earnedWorkDelta"] for e in events),
            "reasons": dict(Counter(e["reason"] for e in closures)), "closedTrials": closures}


def make_analysis(manifest, completion, indexed, seeds, names, archives, origins):
    arms = {}
    for name in names:
        runs = [indexed[s, name] for s in seeds]
        trace = lambda r: r["ecology"]["trace"]
        group = aggregate(runs, trace)
        group["updates"] = sum(row["learningUpdates"] for r in runs for row in r["ecology"]["rows"])
        group["comparisons"] = sum(row["comparisons"] for r in runs for row in r["ecology"]["rows"])
        group["trials"] = trial_metrics(runs)
        group["phaseSource"] = [{"phase": p, "source": c, **aggregate(runs, lambda r, p=p, c=c:
                                     [s for s in trace(r) if (s["phase"], s["source"]) == (p, c)]),
                                  "trials": trial_metrics(runs, lambda e, p=p, c=c: (e["phase"], e["source"]) == (p, c))}
                                 for p in range(4) for c in range(4)]
        for key, predicate in (("pureNoise", lambda s: s["source"] == 2),
                               ("earlyFutureNoise", lambda s: s["source"] == 3 and s["phase"] < 3),
                               ("newFinalUtility", lambda s: s["source"] == 3 and s["phase"] == 3),
                               ("finalPhase", lambda s: s["phase"] == 3)):
            group[key] = aggregate(runs, lambda r, pred=predicate: [s for s in trace(r) if pred(s)])
            group[key]["trials"] = trial_metrics(runs, predicate)
        group["newFinalUtility"]["profitableSeeds"] = sum(v["gross"] > v["burned"] for v in group["newFinalUtility"]["perSeed"])
        for key, purchased in (("rareReturnFirst16Requests", False), ("rareReturnFirst16Purchases", True)):
            group[key] = aggregate(runs, lambda r, purchased=purchased:
                [s for s in trace(r) if s["phase"] == 3 and s["source"] == 1 and (not purchased or s["bought"])][:16])
        group["commonAdaptFirst16Requests"] = aggregate(runs, lambda r:
            [s for s in trace(r) if s["phase"] == 1 and s["source"] == 0][:16])
        group["financing"] = [{"seed": r["seed"], "sources": [{"source": c,
            "firstClosedTick": r["_closedAt"].get(c), "seedSpent": state["seedSpent"],
            "seedRemaining": state["seedRemaining"], "earnedSpent": state["earnedSpent"],
            "attempts": state["trials"], "accepted": state["accepted"], "rejected": state["rejected"],
            "lastStatus": state["status"], "lastObservedSupplierReserve": next(s["supplierReserve"] for s in reversed(trace(r)) if s["source"] == c),
            "postFirstClose": metrics(s for s in trace(r) if s["source"] == c and c in r["_closedAt"] and s["tick"] > r["_closedAt"][c])}
            for c, state in sorted(r["_statesBySource"].items())]} for r in runs]
        group["afterAllStartupClosed"] = [{"seed": r["seed"],
            "allFourClosed": len(r["_closedAt"]) == 4,
            "startTick": max(r["_closedAt"].values()) if len(r["_closedAt"]) == 4 else None,
            **metrics(s for s in trace(r) if len(r["_closedAt"]) == 4 and s["tick"] > max(r["_closedAt"].values()))}
            for r in runs]
        arms[name] = group
    contrasts = {}
    for label, right, primary in (("paired minus unfiltered", UNFILTERED, True), ("paired minus no-seed", NOSEED, True),
                                  ("paired minus existing error", ERROR, False), ("paired minus existing progress", PROGRESS, False)):
        values = [indexed[s, PAIRED]["ecology"]["netPerStep"] - indexed[s, right]["ecology"]["netPerStep"] for s in seeds]
        contrasts[label] = {"left": PAIRED, "right": right, "primary": primary, **estimate(values), "positiveSeeds": sum(v > 0 for v in values)}
    return {"protocol": completion, "settings": manifest["settings"], "seeds": seeds,
            "armDefinitions": manifest["arms"], "arms": arms, "pairedContrasts": contrasts,
            "trialLaw": {k: manifest[k] for k in ("trainingEvents", "validationEvents", "trialWorkCap", "minimumKnownValidation", "retryPeriod", "maximumAttempts")},
            "audit": {"allTaskAccountingChecks": True, "pairedTapesAndSourceSchedules": True,
                      "trialCounterAndSeedFundingChecks": True, "sourceHashesMatch": True,
                      "sourceHashes": manifest["sourceHashes"], "sourceHashOrigins": origins, "archives": archives,
                      "preRunFreeze": manifest.get("_freezeAudit"),
                      "totalTasks": completion["trials"], "pairedCalculationScope": "Every advanced validation event reconstructed from captured pre-feedback paired advice, actual issued command and bound physical gross under the declared opposed-action payout law."},
            "notes": ["All-task endpoint includes holds, startup, failed work and rejected trials.",
                      "Only eight independent seed differences receive descriptive unadjusted t(7) intervals; two-seed pilots have none.",
                      "Fees redistribute conserved funds; actual physical gross minus real burn defines group return.",
                      "Costs are abstract. Candidate replay estimates cannot mint external income.",
                      "Both structures are frozen during the declared shared validation block; candidate open reuses the incumbent projected resource and contributes exactly zero paired gain.",
                      "Phase and source quality are evaluator labels, never learner inputs.",
                      "Burn is assigned to its world request; per-source net is not causal contribution credit.",
                      "Fresh16 windows are descriptive, stop before another promotion and may include online learning after each prediction.",
                      "Reported row updates count base-predictor updates only. Candidate trainingCount counts paid physical training experiences, not successful ordinal updates.",
                      "A promoted candidate can later learn as the current model. Historical trial roots retain live model carrier state; captured validation publications preserve what was evaluated, not a permanent frozen model snapshot.",
                      "Supplier reserve is its last observed request-time balance, not a simultaneous final balance across all services.",
                      "Post-first-closure windows begin at different times and are selection-conditioned; fixed final-phase results are also reported.",
                      "A fee-funded service can remain solvent while the whole system loses physical resource.",
                      "Trial causes inferred from coverage/gain are diagnostics; an incomplete trial alone does not identify why it aborted.",
                      "Saved archive checks cover their declared retained-anchor and paid-gate fixture, not a whole-world restart."]}


def number(value):
    return "—" if value is None else f"{value:.3f}"


def summary(analysis):
    lines = ["# Protected candidate trials: analysis", "", f"{len(analysis['seeds'])} seeds; {analysis['protocol']['trials']:,} world requests. All declared audits passed.",
             "", "Costs are abstract; physical gross minus paid burn is the primary return. Internal fees do not create income.", "",
             "| Arm | Net/request | Gross/request | Burn/request | Coverage | Trials | Promoted | Seed work | Earned work |",
             "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |"]
    for name in NAMES:
        arm = analysis["arms"][name]; t = arm["trials"]
        lines.append(f"| {LABELS[name]} | {number(arm['netPerStep']['mean'])} | {number(arm['grossPerStep']['mean'])} | {number(arm['burnPerStep']['mean'])} | {arm['coverage']:.1%} | {t['launches']} | {t['promotions']} | {t['seedWork']} | {t['earnedWork']} |")
    lines += ["", "## Paired contrasts", "", "| Difference in net/request | Mean | 95% interval | Positive seeds |", "| --- | ---: | --- | ---: |"]
    for label, value in analysis["pairedContrasts"].items():
        interval = value["ci95"]
        ci = f"[{interval[0]:.3f}, {interval[1]:.3f}]" if interval else "None for this pilot"
        lines.append(f"| {label} | {number(value['mean'])} | {ci} | {value['positiveSeeds']}/{value['n']} |")
    lines += ["", "The first two contrasts are primary. Legacy contrasts compare full packages.", "", "## Noise and later usefulness", "",
              "| Arm | Noise promotions | Noise trial work | Newly useful net/request | Profitable seeds | Rare first16 correct/requests |", "| --- | ---: | ---: | ---: | ---: | ---: |"]
    for name in NAMES:
        a = analysis["arms"][name]; noise = a["pureNoise"]["trials"]; new = a["newFinalUtility"]; rare = a["rareReturnFirst16Requests"]
        lines.append(f"| {LABELS[name]} | {noise['promotions']} | {noise['seedWork'] + noise['earnedWork']} | {number(new['netPerStep']['mean'])} | {new['profitableSeeds']}/{len(analysis['seeds'])} | {rare['correct']}/{rare['steps']} |")
    lines += ["", "## Interpretation limits", ""] + ["- " + note for note in analysis["notes"]]
    return "\n".join(lines) + "\n"


def snapshot(directory, manifest, frozen):
    preserved = {}
    prior_path = directory / "source-snapshot.zip"
    if prior_path.is_file():
        with zipfile.ZipFile(prior_path) as prior:
            for name in ("protocol-at-freeze.md", "SNAPSHOT.json"):
                if name in prior.namelist():
                    preserved[name] = prior.read(name)
    freeze_record = directory / "SNAPSHOT.json"
    if freeze_record.is_file():
        payload = freeze_record.read_bytes()
        require("SNAPSHOT.json" not in preserved or preserved["SNAPSHOT.json"] == payload,
                "Existing freeze records disagree; refusing snapshot replacement")
        preserved["SNAPSHOT.json"] = payload
    if "SNAPSHOT.json" in preserved:
        record = json.loads(preserved["SNAPSHOT.json"].decode("utf-8-sig"))
        require("protocol-at-freeze.md" in preserved and
                digest(preserved["protocol-at-freeze.md"]) == record["protocolHash"],
                "Frozen protocol hash differs; refusing snapshot replacement")
    extras = set()
    for folder in (HERE, ROOT / "experiments/UtilityEcology.Tests"):
        for path in folder.rglob("*"):
            if not path.is_file() or any(p in ("bin", "obj", "results", "verification") for p in path.relative_to(ROOT).parts):
                continue
            if path.suffix in (".md", ".py", ".cs", ".csproj", ".json", ".ps1") or path.name == "Program.cs.baseline":
                extras.add(path)
    extras.add(ROOT / "Gojo.slnx")
    temporary = tempfile.NamedTemporaryFile(prefix="seed-snapshot-", suffix=".zip", dir=directory, delete=False)
    temporary.close()
    with zipfile.ZipFile(temporary.name, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for name, payload in frozen.items():
            archive.writestr(name, payload)
        for name, payload in preserved.items():
            archive.writestr(name, payload)
        current = {}
        for path in sorted(extras):
            name = path.relative_to(ROOT).as_posix()
            if name not in frozen:
                payload = path.read_bytes(); archive.writestr(name, payload); current[name] = digest(payload)
        archive.writestr("snapshot-record.json", json.dumps({"frozenSourceHashes": manifest["sourceHashes"],
            "preservedPreRunRecords": {name: digest(payload) for name, payload in preserved.items()},
            "supportingFilesCurrentAtAnalysis": current,
            "note": "C# study sources match the pre-run manifest. Supporting tests/docs/backout/scripts are captured at analysis time, not claimed pre-run frozen."}, indent=2))
    Path(temporary.name).replace(prior_path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", nargs="?", default="results/seed-confirmation-01")
    parser.add_argument("--snapshot", action="store_true")
    args = parser.parse_args()
    path = Path(args.directory); directory = path if path.is_absolute() else HERE / path
    manifest, completion, indexed, seeds, names, archives, frozen, origins = load(directory)
    analysis = make_analysis(manifest, completion, indexed, seeds, names, archives, origins)
    (directory / "analysis.json").write_text(json.dumps(analysis, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    (directory / "SUMMARY.md").write_text(summary(analysis), encoding="utf-8")
    if args.snapshot:
        snapshot(directory, manifest, frozen)
    print(f"Audited {completion['trials']:,} tasks; wrote {directory / 'analysis.json'} and SUMMARY.md")


if __name__ == "__main__":
    main()
