"""Summarize the locked searches and preserve each criterion's separate evidence."""
import collections
import csv
import gzip
import hashlib
import json
import statistics
import sys
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def table(path):
    with path.open(encoding="utf-8-sig",newline="") as f:
        return list(csv.DictReader(f))


def summarize(run):
    done=read(run/"complete.json")
    for name,expected in done["files"].items():
        if hashlib.sha256((run/name).read_bytes()).hexdigest().upper()!=expected:
            raise ValueError(name)
    design=read(run/"design/design.json")
    groups=table(run/"group-comparison.csv")
    pivots=table(run/"criteria-utility.csv")
    verdict=table(run/"verdicts.csv")
    with gzip.open(run/"blind-cases.csv.gz","rt",encoding="utf-8-sig",newline="") as f:
        cases=[r for r in csv.DictReader(f) if r["arm"]=="full"]
    stats=[]
    preference=run.parent/"preference"
    visual_result=read(preference/"reserve-result.json")
    native={x["id"]:x["metrics"] for x in read(preference/"visual-review/key/traces.json")}
    for i,crit in enumerate(design["criteria"]):
        key=crit["key"]
        use=[r for r in pivots if r["key"]==key]
        visual=[]
        for case in visual_result["rows"]:
            pair=case["pair"]
            left,right=native[pair+"L"][i],native[pair+"R"][i]
            if abs(case["target"])==1 and left is not None and right is not None and abs(left-right)>=.02:
                visual.append((left-right)*case["target"]>0)
        stats.append(dict(key=key,group=crit["group"],read=crit["read"],
                          eligible=sum(int(r["eligible_pairs"]) for r in use),
                          up=sum(int(r["positive_pivots"]) for r in use),
                          down=sum(int(r["negative_pivots"]) for r in use),
                          observed=sum(bool(r[key]) for r in cases),cases=len(cases),
                          visual_agree=sum(visual),visual_direction=len(visual)))
    summary=collections.defaultdict(dict)
    for brief in (b["name"] for b in design["briefs"]):
        match=[r for r in groups if r["brief"]==brief]
        summary[brief]["full"]=statistics.mean(float(r["full_mean"]) for r in match)
        for arm in dict.fromkeys(r["arm"] for r in match):
            summary[brief][arm]=statistics.mean(float(r["arm_mean"]) for r in match if r["arm"]==arm)
    out={"means":summary,"criteria":stats,"primary":dict(collections.Counter(r["status"] for r in verdict if r["stratum"]=="all")),
         "strata":dict(collections.Counter(r["status"] for r in verdict if r["stratum"]!="all"))}
    (run/"analysis.json").write_text(json.dumps(out,indent=2),encoding="utf-8")
    lines=["# Which judges affected the drawings?","",
           "A criterion can be requested yet have no evidence to read. It can also reward the same change as several other criteria. The following counts distinguish availability from occasions when a criterion actually tipped a search decision.","",
           "Each row pools the five full briefs and four search replicates. **Available** counts the 480 blind full-finalist drawings on which the criterion supplied a reading. **Compared** counts single-parent training mutations on which the parent or child supplied evidence. **For** means the child beat its parent on the full score but would tie or lose without this criterion. **Against** is the analogous deflection of a losing child. These are pivotal numerical comparisons, including proposals that another elite eventually displaced. The independent training omissions below address a different question.","",
           "The final column is a separate, post-hoc visual check. Of 14 unanimous non-tied choices on a fresh panel, how often did the higher individual reading agree with the observers? Only pairs with both readings present and a difference of at least 0.02 enter that criterion's denominator. These small, overlapping samples concern two AI observers and selected pairs.","",
           "| Perspective | Criterion | What it reads | Available | Compared | For | Against | Visual agreement |","| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |"]
    for r in stats:
        visual=f"{r['visual_agree']}/{r['visual_direction']}" if r["visual_direction"] else "—"
        lines.append(f"| {r['group']} | {r['key']} | {r['read']} | {r['observed']}/{r['cases']} | {r['eligible']:,} | {r['up']} | {r['down']} | {visual} |")
    lines+=["","## What the combinations selected","",
            "Here the full utility of every frozen machine is read under its named brief on the same 24 held-out realizations. Each mean includes four independently selected finalists. Each independently searched arm received the same number of paid proposals and selected its own finalist under its training rule. The foundation and runtime interventions are controls.","",
            "| Selection perspective | "+" | ".join(summary)+" |",
            "| --- |"+" ---: |"*len(summary)]
    for arm in ("full","foundation","random","unseeded","field","craft+field","seam+rhythm",
                "topology+craft","symmetry+rhythm",*["omit-"+g for g in ("craft","rhythm","field","symmetry","topology","seam")],
                "cut","frozen","open"):
        lines.append("| "+arm+" | "+" | ".join(f"{m[arm]:.3f}" for m in summary.values())+" |")
    lines+=["","Cut, frozen and open change the same full winner at execution. Foundation is validation selection from six supplied starting programs. See the [criterion definitions and source evidence](Criteria.md) for what each judgment can establish."]
    return "\n".join(lines)+"\n"


if __name__=="__main__":
    result=summarize(Path(sys.argv[1]))
    Path(sys.argv[2]).write_text(result,encoding="utf-8")
