"""Exploratory same-program comparison of joint sequences with a live or withdrawn learned route."""
import argparse
import csv
import gzip
import statistics
from collections import defaultdict
from pathlib import Path


def load(path):
    with gzip.open(path,"rt",encoding="utf-8-sig",newline="") as f:
        yield from csv.DictReader(f)


def analyze(folder):
    arms=("coupled","disconnect")
    cases=defaultdict(dict)
    for r in load(folder/"blind.csv.gz"):
        if r["arm"] in arms:
            key=(r["family"],r["scenario"],r["rep"])
            cases[key][r["arm"]]=(r["parent"],float(r["broad"]),set())
    for r in load(folder/"joint-uses.csv.gz"):
        if r["arm"] in arms:
            cases[(r["family"],r["scenario"],r["rep"])][r["arm"]][2].add((r["kind"],r["sequence"]))
    records=defaultdict(lambda:defaultdict(list))
    for (family,_,_),pair in cases.items():
        parent,q_live,live=pair["coupled"]
        _,q_cut,cut=pair["disconnect"]
        for key in live.symmetric_difference(cut):
            sign=1 if key in live else -1
            records[(family,)+key][parent].append(sign*(q_live-q_cut))
    path=folder/"joint-paired.csv"
    summary=[]
    with path.open("w",encoding="utf-8",newline="") as out:
        w=csv.writer(out)
        w.writerow(["family","kind","sequence","informative_pairs","parent_groups","positive","negative","within","mean_delta_with_sequence"])
        for key,parents in sorted(records.items()):
            groups=[statistics.mean(ds) for ds in parents.values()]
            plus=sum(x>=.012 for x in groups);minus=sum(x<=-.012 for x in groups)
            counts=sum(map(len,parents.values()))
            row=(*key,counts,len(groups),plus,minus,len(groups)-plus-minus,statistics.mean(groups))
            w.writerow(row);summary.append(row)
    informative=[r for r in summary if r[4]>=8 and r[3]>=24]
    positive=sorted((r for r in informative if r[-1]>0),key=lambda r:r[-1],reverse=True)[:5]
    negative=sorted((r for r in informative if r[-1]<0),key=lambda r:r[-1])[:5]
    lines=["# Which joint sequences accompanied a better result?","",
        "We replayed the same frozen machine on the same physical basis with live feedback and with its actuators silenced. A sequence contributes only when it appears in exactly one member of a pair. Its signed difference is that member’s broad utility minus the other member’s utility. We averaged within each calibration parent, then across parents. These are retrospective associations. The intervention can alter many strokes, so a favorable association cannot establish that a particular joint caused an improvement.","",
        f"Of {len(summary)} family/sequence pairs separated at least once, {len(informative)} had at least 24 informative comparisons across eight or more calibration parents. Below are up to five positive and five negative associations meeting that exposure requirement. The full CSV retains rare cases. Positive and negative parent counts use a 0.012 margin. Absence of an informative pair means the intervention did not separate that sequence in this cohort.","",
        "| Family | Joint word | Informative pairs | Parents, positive / negative | Mean difference |",
        "| --- | --- | ---: | ---: | ---: |"]
    for r in positive+negative:
        lines.append(f"| {r[0]} | {r[1]} {r[2]} | {r[3]} | {r[4]}, {r[5]} / {r[6]} | {r[8]:+.3f} |")
    lines+=["","Turn words preserve two consecutive turns (0 straight, 1 counterclockwise eighth-turn, 2 quarter-turn, 6 clockwise quarter-turn, 7 clockwise eighth-turn). Fork words list incoming and outgoing ray masks around one incoming ray; bit 4 of the outgoing mask continues straight. Repetition of the same shape in two arms is not counted. [All comparisons](joint-paired.csv)."]
    (folder/"JointAssociations.md").write_text("\n".join(lines)+"\n",encoding="utf-8")
    return len(cases),len(summary),len(informative)


if __name__=="__main__":
    args=argparse.ArgumentParser(description=__doc__);args.add_argument("run",type=Path)
    print(analyze(args.parse_args().run))
