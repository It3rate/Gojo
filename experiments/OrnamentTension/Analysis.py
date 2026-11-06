"""Read the sealed tournament, including adverse outcomes. No machine is selected here."""
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
    opener=gzip.open if path.suffix==".gz" else open
    with opener(path,"rt",newline="",encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def mean(rows,key):
    return statistics.mean(float(r[key]) for r in rows)


def write_table(path,rows):
    with path.open("w",encoding="utf-8",newline="") as f:
        writer=csv.DictWriter(f,list(rows[0]))
        writer.writeheader();writer.writerows(rows)


def analyze(root):
    done=read(root/"blind.lock")
    for name,expected in done["files"].items():
        if hashlib.sha256((root/name).read_bytes()).hexdigest().upper()!=expected:
            raise ValueError(f"Sealed file differs: {name}")
    lock=read(root/"finalists.json")
    design=read(root/"design/settings.json")
    rows=table(root/"blind.csv.gz")
    comparisons=table(root/"contrasts.csv")
    families=[x["key"] for x in design["engines"]]
    members=collections.defaultdict(list)
    for r in rows:
        members[r["family"],r["arm"],int(r["slots"])].append(r)
    columns=["broad","matched_reference","wrong_reference","travel","range","slot-reach","tips","forks","merges",
             "crossings","overlaps","no-overdraw","whole-course-span","shared-seam","turn-variety","motif-mirror",
             "alternating-mirror","shape-novelty","cell-variation","winding-terminal","offered","accepted","blocked",
             "in_flight","actions","route_changes","relief","misses","pen_debt","machine_debt","frieze_debt"]
    summary=[dict(family=fam,arm=arm,slots=slot,cases=len(group),**{c:mean(group,c) for c in columns})
             for (fam,arm,slot),group in sorted(members.items())]
    write_table(root/"means.csv",summary)
    panel_reads=[]
    for p in lock["panels"]:
        for c in p["panels"]:
            weights=bytes.fromhex(c["selected"])
            panel_reads.append(dict(family=families[p["family"]],replicate=p["replicate"],slots=c["slots"],
                panel=" + ".join(name for name,n in zip(design["judges"],weights) if n),
                size=sum(n>0 for n in weights),calibration_gain=c["validation"]-c["declaredValue"],
                **dict(zip(design["judges"],weights))))
    write_table(root/"panel-use.csv",panel_reads)
    # Within the exact same physical input and family, compare the four selected
    # search replicates only where at least one uses the sequence and one does not.
    grouped=collections.defaultdict(list)
    for r in rows:
        if r["arm"]=="coupled":grouped[r["family"],r["scenario"]].append(r)
    sequences=collections.defaultdict(set)
    for r in table(root/"joint-uses.csv.gz"):
        if r["arm"]=="coupled":sequences[r["kind"],r["sequence"]].add((r["family"],r["scenario"],r["rep"]))
    joints=[]
    for (kind,key),present in sorted(sequences.items()):
        deltas=[]
        for (family,scenario),people in grouped.items():
            yes=[p for p in people if (family,scenario,p["rep"]) in present]
            no=[p for p in people if (family,scenario,p["rep"]) not in present]
            if yes and no:deltas.append(mean(yes,"broad")-mean(no,"broad"))
        joints.append(dict(kind=kind,sequence=key,containing_drawings=len(present),matched_inputs=len(deltas),
            mean_association=statistics.mean(deltas) if deltas else None))
    write_table(root/"joint-combinations.csv",joints)
    by_contrast={}
    for name in dict.fromkeys(c["contrast"] for c in comparisons):
        g=[c for c in comparisons if c["contrast"]==name]
        by_contrast[name]=dict(delta=mean(g,"mean_delta"),changed=sum(int(c["changed_material"]) for c in g),
                               verdicts=dict(collections.Counter(c["status"] for c in g)))
    data=dict(status=dict(collections.Counter(c["status"] for c in comparisons)),contrast=by_contrast,
              panel_sizes=dict(collections.Counter(c["size"] for c in panel_reads)),
              panel_counts=dict(collections.Counter(c["panel"] for c in panel_reads)),
              participation={name:sum(c[name]>0 for c in panel_reads) for name in design["judges"]},
              shares={name:sum(c[name] for c in panel_reads) for name in design["judges"]},
              source_similarity=mean([r for r in rows if r["arm"]=="coupled"],"matched_reference"),
              coupled={s:{c:mean([r for r in rows if r["arm"]=="coupled" and int(r["slots"])==s],c) for c in columns} for s in (2,5,9)})
    (root/"analysis.json").write_text(json.dumps(data,indent=2),encoding="utf-8")
    return rows,data


if __name__=="__main__":
    analyze(Path(sys.argv[1]))
