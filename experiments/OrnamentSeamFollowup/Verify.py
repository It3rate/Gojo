"""Replay a completed seam cue follow-up through the sealed primary runtime.

python experiments/OrnamentSeamFollowup/Verify.py [artifacts/local-ornament]

Only verification/ receives output. Training and winner selection are never run.
"""
import csv
import gzip
import hashlib
import json
import math
from collections import Counter, defaultdict
from pathlib import Path
import subprocess
import sys
import tempfile
from xml.sax.saxutils import escape

U64 = (1 << 64) - 1
CUES = ("original", "seam", "decoy")
CONTRASTS = {
    "learn-location": ("seam", "seam", "decoy", "decoy"),
    "displace-same-winner": ("seam", "seam", "seam", "decoy"),
    "supplied-marker-vs-original": ("seam", "seam", "original", "original"),
    "marker-vs-hold": ("seam", "seam", "idle", "seam"),
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest().upper()


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def read_csv(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def packed(value):
    return json.dumps(value, separators=(",", ":"), ensure_ascii=False).encode("utf-8")


def decoy(group, width, step, variant, seed):
    # Same published FNV offset and mix as the primary ProposalRandom.Key.
    h = ((14695981039346656037 ^ seed) * 1099511628211) & U64
    for c in group:
        h = ((h ^ ord(c)) * 1099511628211) & U64
    edge = min(step + 1, width // 2)
    site = edge + h % (width - 2 * edge)
    return site if variant == 0 else width - 1 - site


def input_for(row, cue, seed):
    fields = {name: int(row[column]) for name, column in (
        ("Width", "width"), ("TargetGap", "target_gap"), ("GoalBits", "goal_bits"),
        ("Stock", "stock"), ("EditBudget", "edit_budget"), ("Beats", "beats"),
        ("InterventionBeat", "intervention_beat"), ("Start", "start"))}
    for name, column in (("Seed", "seed_hex"), ("Initial", "initial_hex"), ("Protected", "protected_hex"),
                         ("Hidden", "hidden_hex"), ("InterventionXor", "intervention_xor_hex")):
        fields[name] = int(row[column], 16)
    marker = -1
    if cue != "original":
        marker = 0 if cue == "seam" else decoy(row["parent"], fields["Width"], fields["TargetGap"],
                                               int(row["variant"]), seed)
        fields["Seed"] = 1 << marker
    return fields, marker


def score(frame, result):
    # Independent conventional examiner: seam wrap gap plus penalty for
    # changing interior sites. Reconstruct both conservation and capacity.
    width, step, ink = frame["Width"], frame["TargetGap"], result["Ink"]
    valid = (1 << width) - 1
    n = (ink & valid).bit_count()
    bad = ((ink ^ frame["Initial"]) & frame["Protected"]).bit_count() + (ink & ~valid).bit_count()
    bad += int(frame["Initial"].bit_count() + frame["Stock"] + result["ExternalDelta"] != n + result["Stock"])
    bad += int(result["Stock"] < 0 or not 0 <= result["Edits"] <= result["Attempts"] <= min(frame["EditBudget"], frame["Beats"]))
    if n < 2:
        error = 1000
    else:
        first = (ink & -ink).bit_length() - 1
        last = ink.bit_length() - 1
        gap = width + first - last
        edge = min(step + 1, width // 2)
        side = (1 << edge) - 1
        outside = side | (side << (width - edge))
        denom = max(step, width // 3)
        error = (1000 * abs(gap - step) + denom // 2) // denom + 100 * ((frame["Initial"] ^ ink) & ~outside).bit_count()
    return (max(0, min(1000, 1000 - error)) if bad == 0 else 0, error, bad, bad == 0 and error == 0)


def run_fresh(jobs, frozen_dll, temp):
    inputs = temp / "inputs.json"
    inputs.write_bytes(packed(jobs))
    fast = temp / "fast.json"
    subprocess.run(["dotnet", str(frozen_dll), "--ornament-campaign", "replay", str(inputs), str(fast)],
                   cwd=temp, check=True)
    # Compile a minimal new host for the original, independently evaluated
    # Gojo BitRelation / PassageLaw oracle path. No search project is referenced.
    project = temp / "NativeOracle.csproj"
    project.write_text(f"""
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>
<TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
<ItemGroup><Reference Include="PrimitiveMeasure"><HintPath>{escape(str(frozen_dll))}</HintPath>
<Private>true</Private></Reference></ItemGroup></Project>""", encoding="utf-8")
    (temp / "Program.cs").write_text("""
using System.Text.Json;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;
var jobs = JsonSerializer.Deserialize<CampaignCli.ReplayInput[]>(File.ReadAllBytes(args[0]));
var results = jobs!.Select(j => OrnamentProgram.Decode(Convert.FromHexString(j.ProgramHex))
    .Compile().RunReference(j.Input, j.Wire)).ToArray();
File.WriteAllBytes(args[1],JsonSerializer.SerializeToUtf8Bytes(results));
""", encoding="utf-8")
    subprocess.run(["dotnet", "build", str(project), "-c", "Release", "-v", "quiet"], cwd=temp, check=True)
    output = temp / "oracle.json"
    built = temp / "bin" / "Release" / "net10.0"
    require((built / "PrimitiveMeasure.dll").read_bytes() == frozen_dll.read_bytes(), "Oracle kernel identity")
    subprocess.run(["dotnet", str(built / "NativeOracle.dll"), str(inputs), str(output)], cwd=temp, check=True)
    fast_rows, oracle_rows = load(fast), load(output)
    require(len(oracle_rows) == len(fast_rows) == len(jobs), "Replay length")
    for i, (a, b) in enumerate(zip(fast_rows, oracle_rows)):
        require(a == b, f"Native reference disagreement on job {i}: {a} vs {b}")
    return fast_rows


def verify(root):
    primary, seam = root / "primary", root / "seam-location"
    frozen_dll = root / "runtime-1" / "PrimitiveMeasure.dll"
    complete = load(seam / "reserve" / "complete.json")
    primary_seal = load(primary / "protocol.lock.json")
    seal, lock = load(seam / "seal.json"), load(seam / "frozen.json")
    require(primary_seal["RuntimeHash"] == seal["KernelHash"] == digest(frozen_dll.read_bytes()), "Kernel seal")
    require(primary_seal["Manifest"]["episodes.csv"] ==
            digest((primary / "manifest" / "episodes.csv").read_bytes()), "Episode manifest")
    require(seal["SpecHash"] == digest((seam / "Protocol.md").read_bytes()), "Prospective spec")
    require(seal["RuntimeHash"] == digest((root / "runtime-seam" / "OrnamentSeamFollowup.dll").read_bytes()), "Seam apparatus seal")
    require(seal["PrimaryFreezeHash"] == digest((primary / "frozen.json").read_bytes()), "Primary winner lock")
    require(seal["PrimaryAssayHash"] == digest((primary / "test" / "complete.json").read_bytes()), "Primary assay seal")
    require(lock["SealHash"] == digest(packed(seal)), "Follow-up experiment identity")
    require(lock["WinnerHash"] == complete["WinnerHash"] == digest(packed(lock["Winners"])), "Seam winner lock")
    for file, h in complete["Files"].items():
        require(digest((seam / "reserve" / file).read_bytes()) == h, f"Completed assay {file}")

    episodes = {(r["parent"], int(r["variant"])): r for r in read_csv(primary / "manifest" / "episodes.csv")
                if r["split"] == "reserve" and r["goal"] == "seam"}
    winners = {(r["Replicate"], r["Cue"]): r["GenomeHex"] for r in lock["Winners"]}
    require(len(winners) == 12 and len(episodes) == 96, "Expected winner/cohort cardinality")
    first_programs = set()
    for rep in range(seal["Replicates"]):
        with gzip.open(seam / f"pool-{rep}.json.gz", "rt", encoding="utf-8") as stream:
            pool = json.load(stream)
        candidates = pool["Candidates"]
        require(len(candidates) == seal["Proposals"] and pool["Replicate"] == rep, "Pool cardinality")
        require(digest(packed(candidates)) == pool["PoolHash"] == lock["TrainingHashes"][rep], "Pool ancestry")
        first_programs.add(candidates[0]["GenomeHex"])
        for row in lock["Winners"]:
            if row["Replicate"] != rep:
                continue
            proposal = candidates[row["Id"]]
            require(proposal["GenomeHex"] == row["GenomeHex"] and
                    proposal["Training"][CUES.index(row["Cue"])] == row["Training"], "Winner differs from its proposal")
    require(first_programs == {"01000000000000"}, "Every first comparison must use the native idle machine")
    no_edit = first_programs.pop()

    jobs, ids, cases = [], [], {}
    recorded = read_csv(seam / "reserve" / "cases.csv")
    for row in recorded:
        key = (int(row["replicate"]), row["training_cue"], row["executed_cue"], row["group"], int(row["variant"]))
        rep, train, cue, parent, variant = key
        episode = episodes[parent, variant]
        frame, marker = input_for(episode, cue, seal["Seed"])
        require(key not in cases and cue in CUES, f"Duplicate/invalid case {key}")
        require(int(row["marker"]) == marker and row["family"] == episode["family"] and int(row["width"]) == frame["Width"]
                and int(row["start"]) == frame["Start"], f"Input provenance {key}")
        for column, field in (("marker_visible", "Hidden"), ("marker_ink", "Initial"), ("marker_protected", "Protected")):
            present = marker >= 0 and bool(frame[field] & (1 << marker))
            wanted = "" if marker < 0 else str(not present if column == "marker_visible" else present)
            require(row[column] == wanted, f"Cue exposure {key}:{column}")
        cases[key] = row
        ids.append(("live", key))
        jobs.append({"ProgramHex": winners[rep, train], "Input": frame, "Wire": 0})
    require(len(cases) == complete["Cases"] == len(winners) * len(CUES) * len(episodes), "Every treatment cell present")
    # Cut controls add a causal intervention, wholly outside the scored ledger.
    for i, (_, key) in enumerate(ids.copy()):
        ids.append(("cut", key))
        jobs.append({**jobs[i], "Wire": 1})
    for (parent, variant), episode in episodes.items():
        for cue in CUES:
            frame, _ = input_for(episode, cue, seal["Seed"])
            ids.append(("idle", (cue, parent, variant)))
            jobs.append({"ProgramHex": no_edit, "Input": frame, "Wire": 0})

    with tempfile.TemporaryDirectory(prefix="gojo-seam-verifier-") as temp:
        results = run_fresh(jobs, frozen_dll, Path(temp))
    live, cuts, idle, replay_rows = {}, {}, {}, []
    for (kind, key), job, result in zip(ids, jobs, results):
        value, error, violations, exact = score(job["Input"], result)
        require(violations == 0, f"Material accounting {kind} {key}")
        if kind == "live":
            row = cases[key]
            require(result["Ink"] == int(row["ink_hex"], 16) and
                    all(result[field] == int(row[field.lower()]) for field in ("Stock", "Attempts", "Blocked", "Edits")) and
                    (value, error, violations, str(exact)) == (int(row["utility"]), int(row["error"]), int(row["violations"]), row["exact"]),
                    f"Recorded case differs {key}")
            live[key] = (value, result)
        elif kind == "idle":
            require(result["Edits"] == result["Attempts"] == result["Blocked"] == 0, f"Idle moved {key}")
            idle[key] = (value, result)
        else:
            cuts[key] = result
        replay_rows.append({"kind": kind, "key": key, **result, "utility": value})

    for (rep, train, cue, parent, variant), row in cases.items():
        expected_idle = idle[cue, parent, variant][0]
        require(int(row["idle_utility"]) == expected_idle and int(row["delta_vs_idle"]) == int(row["utility"]) - expected_idle,
                "Idle comparison")
        require(cuts[rep, train, cue, parent, variant] == cuts[rep, train, "original", parent, variant],
                "An unconnected writer responded to a changed port-10 cue")
    summary = verify_summaries(seam, episodes, live, idle)
    output = root / "verification"
    output.mkdir(exist_ok=True)
    replay_file = output / "seam-native-replays.json.gz"
    replay_file.write_bytes(gzip.compress(packed(replay_rows), mtime=0))
    certificate = {"kernel_sha256": seal["KernelHash"], "winner_lock_hash": lock["WinnerHash"],
        "assay_hashes_verified": complete["Files"], "winners": len(winners), "goal": "seam", "split": "reserve",
        "parent_groups": len({parent for parent, _ in episodes}), "episodes": len(episodes),
        "live_cases_matched": len(live), "idle_controls": len(idle), "cut_controls": len(cuts),
        "reference_parity_jobs": len(results), "frozen_fields_compared": ["Ink","Stock","Attempts","Blocked","Edits"],
        "trace_contract": "Follow-up cases.csv stored no old trace hash. The fresh fast and reference paths agree on every TraceHash and all other RailResult fields.",
        "controls": "All three port-10 cue versions yielded exactly the same RailResult with the wire cut, including trace; all idle programs held.",
        "score_and_verdicts": summary, "retained_replay_sha256": digest(replay_file.read_bytes())}
    (output / "seam-replay-certificate.json").write_text(json.dumps(certificate, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(certificate, indent=2))


def close(a, b, label):
    require(math.isclose(float(a), float(b), abs_tol=1e-9, rel_tol=1e-12), f"{label}: {a} != {b}")


def verify_summaries(seam, episodes, live, idle):
    matrix = {}
    for r in read_csv(seam / "reserve" / "matrix.csv"):
        key = (r["training_cue"], r["executed_cue"])
        require(key not in matrix, "Duplicate matrix entry")
        matrix[key] = r
    require(len(matrix) == 9, "Crossover matrix incomplete")
    for (train, cue), row in matrix.items():
        selected = [(v, z, idle[k[2:]][0]) for k, (v, z) in live.items() if k[1:3] == (train, cue)]
        n = len(selected)
        require(n == int(row["cases"]), "Matrix denominator")
        for column, values in (
            ("mean_utility", [v for v,z,i in selected]), ("mean_idle", [i for v,z,i in selected]),
            ("mean_delta_vs_idle", [v-i for v,z,i in selected]), ("mean_edits", [z["Edits"] for v,z,i in selected])):
            close(row[column],sum(values)/n,column)
        counts = Counter((v>i)-(v<i) for v,z,i in selected)
        require((counts[1],counts[0],counts[-1]) == tuple(int(row[k]) for k in ("improved","tied","regressed")), "Matrix counts")

    groups = {(r["contrast"], r["group"]): r for r in read_csv(seam / "reserve" / "groups.csv")}
    verdicts = {r["contrast"]: r for r in read_csv(seam / "reserve" / "verdicts.csv")}
    require(set(verdicts) == set(CONTRASTS) and len(groups) == 192, "Comparison coverage")
    certified = []
    for name, (left_train,left_cue,right_train,right_cue) in CONTRASTS.items():
        compared = defaultdict(list)
        for parent, variant in episodes:
            for rep in range(4):
                l, lr = live[rep,left_train,left_cue,parent,variant]
                r, rr = idle[right_cue,parent,variant] if right_train == "idle" else live[rep,right_train,right_cue,parent,variant]
                changed = any(lr[f] != rr[f] for f in ("Ink","Stock","Edits"))
                compared[parent].append((l,r,changed))
        positive = negative = changed_total = 0
        means = []
        for parent, pairs in compared.items():
            row = groups[name,parent]
            require(int(row["pairs"]) == len(pairs) == 8, "Parent pairing")
            require(episodes[parent,0]["family"] == row["family"], "Parent stratum")
            left=sum(l for l,r,c in pairs)/len(pairs); right=sum(r for l,r,c in pairs)/len(pairs)
            d=left-right
            changes=sum(c for l,r,c in pairs)
            for column, value in (("mean_left",left),("mean_right",right),("mean_delta",d)):
                close(row[column],value,column)
            require(int(row["material_changed"])==changes,"Parent material changes")
            positive += d>=20; negative += d<=-20; changed_total += changes; means.append(d)
        n=len(means)
        status="supported" if positive*3>=2*n else "contrary" if negative*3>=2*n else "inconclusive"
        row=verdicts[name]
        require((int(row["margin"]),int(row["parent_groups"]),int(row["positive"]),int(row["negative"]),int(row["within"]),
                 int(row["material_changed"]),row["status"])==(20,n,positive,negative,n-positive-negative,changed_total,status), "Verdict "+name)
        mean=sum(means)/n
        close(row["mean_delta"],mean,name)
        certified.append({"contrast":name,"parents":n,"positive":positive,"negative":negative,"mean_delta":mean,"status":status})
    return certified


if __name__ == "__main__":
    verify(Path(sys.argv[1] if len(sys.argv)>1 else "artifacts/local-ornament").resolve())
