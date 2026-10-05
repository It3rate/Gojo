"""Host verification of frozen campaign JSON; never executes or feeds actors.

Run with Python 3.10+ from any directory. The checkout is located from this
file. Only results/wake-campaign/audit-result.json is written. Arithmetic here
independently checks recorded native outcomes; it is not experiment reasoning.
"""
from __future__ import annotations
import collections
import hashlib
import json
from pathlib import Path
import sys

ROOT = next(parent for parent in Path(__file__).resolve().parents if (parent / "Gojo.slnx").is_file())
NEW = ROOT / "experiments/ChangeSubscriptions/results/wake-campaign"
OLD = ROOT / "experiments/ChangeSubscriptions/results/first-campaign"
failures: list[str] = []
checks = 0

def check(ok, context):
    global checks
    checks += 1
    if not ok and len(failures) < 300:
        failures.append(context)

def sign(value):
    return (value > 0) - (value < 0)

def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def rows(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line]

def legacy(value):
    if isinstance(value, dict):
        return {k: legacy(v) for k, v in value.items() if k != "Wake"}
    if isinstance(value, list):
        return [legacy(v) for v in value]
    return value

def legacy_row(row):
    return {**{k: row[k] for k in ("Step", "Phase", "Level", "Opportunity", "ActualDelta", "NextPrediction", "Views")},
        "Outcomes": [{k: x[k] for k in ("Prior", "Error", "Reference", "Used", "Evaluated")} for x in row["Outcomes"]],
        "Uses": [{k: x[k] for k in ("Attached", "Rung", "RetryAt", "Notifications")} for x in row["Uses"]]}

if not (NEW / "comparison.json").exists():
    print(json.dumps({"status": "waiting", "path": str(NEW)}))
    raise SystemExit(2)

runs = load(NEW / "comparison.json")
old_runs = {(r["Fixture"], r["Policy"]): r for r in load(OLD / "comparison.json")}
manifest = load(NEW / "manifest.json")
protocol = (NEW / "protocol.snapshot.md").read_bytes()
check(manifest["ProtocolFingerprint"] == hashlib.sha256(protocol).hexdigest().upper(), "frozen protocol fingerprint")
check(protocol.replace(b"\r\n", b"\n") == (ROOT / "experiments/ChangeSubscriptions/WAKE_PROTOCOL.md").read_bytes().replace(b"\r\n", b"\n"),
      "current protocol matches frozen content independently of checkout line endings")
check(len(runs) == 18, "18 declared runs")
check(len({(r["Fixture"], r["Policy"]) for r in runs}) == 18, "unique runs")
check(len({r["Fixture"] for r in runs}) == 6, "six fixtures")
check({r["Policy"] for r in runs} == {"fixed2", "escalating32", "escalating32-wake2"}, "three declared policies")
inspected = 0
matched_controls = 0
table = []
phase_table = []

for run in runs:
    fixture, policy = run["Fixture"], run["Policy"]
    ctx = f"{fixture}/{policy}"
    data = rows(NEW / f"{fixture}-{policy}" / "arrivals.jsonl")
    inspected += len(data)
    if (fixture, policy) in old_runs:
        old = old_runs[fixture, policy]
        check(legacy(run["State"]) == old["State"], f"{ctx} legacy final semantics")
        check(legacy(run["Phases"]) == old["Phases"], f"{ctx} legacy phase semantics")
        check(run["PendingPaid"] == old["PendingPaid"], f"{ctx} legacy pending")
        old_data = rows(OLD / f"{fixture}-{policy}" / "arrivals.jsonl")
        check([legacy_row(x) for x in data] == old_data, f"{ctx} all legacy row semantics")
        matched_controls += 1

    totals = [collections.Counter(), collections.Counter()]
    phase_totals = collections.defaultdict(lambda: [collections.Counter(), collections.Counter()])
    review = [dict(count=0, benefit=0, rung=0, deadline=0, completed=0) for _ in range(2)]
    grant_counts = [collections.Counter(), collections.Counter()]
    issue_counts = [collections.Counter(), collections.Counter()]
    previous = None
    previous_level = None
    progress = 0
    for row in data:
        step = row["Step"]
        context = f"{ctx}@{step}"
        actual = row["Level"] - previous_level if row["Level"] is not None and previous_level is not None else None
        predicted = actual + 2 * sign(actual) if actual is not None else None
        progress += int(actual is not None)
        check(row["ActualDelta"] == actual, f"{context} consecutive-input actual")
        check(row["NextPrediction"] == predicted, f"{context} causal supplied predictor")
        check(row["Opportunity"] == progress, f"{context} local validation progress")
        for i in range(3):
            before_view = previous["Views"][i] if previous else dict(Held=0, Valid=0, Revision=0)
            held = before_view["Held"] if predicted is None else (sign(predicted) if i == 0 else predicted if i == 1 else sign(predicted) * (abs(predicted) // 4))
            valid = int(predicted is not None)
            revision = before_view["Revision"] + int(held != before_view["Held"] or valid != before_view["Valid"])
            check(row["Views"][i] == dict(Held=held, Valid=valid, Revision=revision), f"{context}/{i} publication law")
        for i in range(2):
            target = totals[i]
            phase_target = phase_totals[row["Phase"]][i]
            use = row["Uses"][i]
            outcome = row["Outcomes"][i]
            old_use = previous["Uses"][i] if previous else None
            prior = None
            if previous and old_use["Attached"] and previous["NextPrediction"] is not None:
                prior = sign(previous["NextPrediction"]) if i == 0 else previous["NextPrediction"]
            old_early = bool(previous and previous["IssuedEarly"][i])
            check(outcome["Prior"] == prior, f"{context}/{i} frozen prior forecast")
            check(outcome["Used"] == int(prior is not None), f"{context}/{i} prior subscription")
            check(outcome["Early"] == old_early, f"{context}/{i} frozen early provenance")
            resolved = actual is not None and previous is not None
            check(outcome["Evaluated"] == int(resolved), f"{context}/{i} evaluated availability")
            paid = predicted is not None and use["Attached"]
            early = bool(row["IssuedEarly"][i])
            check(not early or paid, f"{context}/{i} early implies paid issue")
            check(not paid or early == (progress < use["RetryAt"]), f"{context}/{i} ordinary vs early eligibility")
            for t in (target, phase_target):
                t["Paid"] += int(paid)
                t["EarlyIssued"] += int(early)
            if resolved:
                selected = sign(actual) if i == 0 else actual
                chosen = prior if prior is not None else 0
                loss, baseline = abs(chosen - selected), abs(selected)
                check(outcome["Error"] == loss and outcome["Reference"] == baseline, f"{context}/{i} selected loss")
                for t in (target, phase_target):
                    t["Error"] += loss
                    t["ReferenceError"] += baseline
                    if prior is not None:
                        benefit = baseline - loss
                        t["Evaluated"] += 1
                        t["Benefit"] += benefit
                        t["Helpful" if benefit > 0 else "Harmful" if benefit < 0 else "Neutral"] += 1
                    if old_early:
                        t["EarlyError"] += loss
                        t["EarlyReferenceError"] += baseline
                        t["EarlyEvaluated"] += 1
                if prior is not None:
                    r = review[i]
                    r["count"] += 1
                    r["benefit"] += baseline - loss
                    if r["count"] == 4:
                        if r["benefit"]:
                            r["rung"] = min(1 if policy == "fixed2" else 5, max(0, r["rung"] - sign(r["benefit"])))
                            r["deadline"] = progress + [0, 2, 4, 8, 16, 32][r["rung"]]
                        r["count"] = r["benefit"] = 0
                        r["completed"] += 1
            else:
                check(outcome["Error"] is None and outcome["Reference"] is None, f"{context}/{i} missing remains open")
                for t in (target, phase_target):
                    t["Unknown"] += int(prior is not None)
                    t["EarlyUnknown"] += int(old_early)
            r = review[i]
            check((use["Rung"], use["RetryAt"], use["CompletedBlocks"], use["BlockCount"], use["BlockBenefit"]) ==
                (r["rung"], r["deadline"], r["completed"], r["count"], r["benefit"]), f"{context}/{i} review unaffected by permission")
            check(not old_use or use["Notifications"] >= old_use["Notifications"], f"{context}/{i} notification monotonic")
            w = use["Wake"]
            if w is not None:
                ow = old_use["Wake"] if old_use else None
                check(w["Watching"] == (progress < use["RetryAt"]), f"{context}/{i} dormant attachment lifecycle")
                check(0 <= w["Tokens"] <= 2, f"{context}/{i} token bounds")
                if w["Watching"]:
                    check((w["Epoch"], w["EpochDeadline"]) == (use["CompletedBlocks"], use["RetryAt"]), f"{context}/{i} epoch linkage")
                for key in ("Notices", "Syncs", "Followups", "RevisionPolls", "Grants", "Issued", "Suppressed", "Error", "ReferenceError", "Evaluated", "Unknown"):
                    check(w[key] >= (ow[key] if ow else 0), f"{context}/{i} monotonic {key}")
                epoch = (w["Epoch"], w["EpochDeadline"])
                grant_delta = w["Grants"] - (ow["Grants"] if ow else 0)
                issue_delta = w["Issued"] - (ow["Issued"] if ow else 0)
                check(grant_delta in (0, 1), f"{context}/{i} grant increment")
                check(issue_delta == int(early), f"{context}/{i} issue consumption")
                grant_counts[i][epoch] += grant_delta
                issue_counts[i][epoch] += issue_delta
                check(grant_counts[i][epoch] <= 1 and issue_counts[i][epoch] <= 2, f"{context}/{i} epoch grant/issue caps")
                check(not early or (w["GrantUsed"] and w["Watching"]), f"{context}/{i} early current grant")
                check(w["Issued"] == target["EarlyIssued"] and w["Evaluated"] == target["EarlyEvaluated"] and w["Unknown"] == target["EarlyUnknown"], f"{context}/{i} live early ledger")
                check(w["Error"] == target["EarlyError"] and w["ReferenceError"] == target["EarlyReferenceError"] and w["Benefit"] == w["ReferenceError"]-w["Error"], f"{context}/{i} live early loss")
            else:
                check(not early, f"{context}/{i} control no early permission")
        previous, previous_level = row, row["Level"]
    for i, c in enumerate(run["State"]["Consumers"]):
        target = totals[i]
        for key in ("Paid", "Error", "ReferenceError", "Evaluated", "Benefit", "Helpful", "Harmful", "Neutral", "Unknown"):
            check(c[key] == target[key], f"{ctx}/{i} summary {key}")
        check(c["Paid"] == c["Evaluated"] + c["Unknown"] + run["PendingPaid"][i], f"{ctx}/{i} paid reconciliation")
        check(c["Benefit"] == c["ReferenceError"] - c["Error"], f"{ctx}/{i} comparative benefit")
        check(c["CostlyTies"] == c["Neutral"], f"{ctx}/{i} paid ties")
        w = c["Wake"]
        if w:
            check(w["Issued"] == w["Evaluated"] + w["Unknown"] + run["PendingEarly"][i], f"{ctx}/{i} early reconciliation")
            check(w == data[-1]["Uses"][i]["Wake"], f"{ctx}/{i} final wake state")
    for phase in run["Phases"]:
        for i in range(2):
            target = phase_totals[phase["Phase"]][i]
            for key in ("Paid", "Error", "ReferenceError", "Evaluated", "Benefit", "Helpful", "Harmful", "Neutral", "Unknown"):
                check(phase["Changes"][i][key] == target[key], f"{ctx}/{phase['Phase']}/{i} phase {key}")
            w = phase["Changes"][i]["Wake"]
            if w:
                for key in ("Issued", "Error", "ReferenceError", "Evaluated", "Unknown"):
                    check(w[key] == target["Early"+key], f"{ctx}/{phase['Phase']}/{i} early phase {key}")
        phase_table.append(dict(Fixture=fixture, Policy=policy, Phase=phase["Phase"], Amount=phase["Changes"][1]))
    table.append(dict(Fixture=fixture, Policy=policy, Consumers=run["State"]["Consumers"],
        GraphSamples=run["GraphSamples"], NativeAllocationBytes=run["NativeAllocationBytes"],
        ReportAllocationBytes=run["ReportAllocationBytes"], NativeMilliseconds=run["NativeMilliseconds"],
        ReportMilliseconds=run["ReportMilliseconds"], NativeTransportBytes=run["NativeTransportBytes"]))

for fixture in {r["Fixture"] for r in runs}:
    group = [r for r in runs if r["Fixture"] == fixture]
    for key in ("Arrival", "Opportunity", "Views", "CoarseNotifications"):
        check(len({json.dumps(r["State"][key], sort_keys=True) for r in group}) == 1, f"{fixture} matched {key}")
    for i in range(2):
        check(len({r["State"]["Consumers"][i]["ReferenceError"] for r in group}) == 1, f"{fixture}/{i} matched baseline")

result = dict(Status="pass" if not failures else "failed", Checks=checks, Runs=len(runs),
    Arrivals=inspected, MatchedOriginalControls=matched_controls, Failures=failures,
    Results=table, Phases=phase_table)
destination = NEW / "audit-result.json"
destination.write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps({k: v for k, v in result.items() if k not in ("Results", "Phases")}))
raise SystemExit(bool(failures))
