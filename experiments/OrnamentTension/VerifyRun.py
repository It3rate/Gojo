"""Recompute grouped claims and coarse-port ambiguity from the recorded rows."""
from collections import defaultdict
import hashlib
import json
import statistics
import sys
from pathlib import Path
from Analysis import read,table


def verify(root):
    blind=table(root/"blind.csv.gz")
    contrasts=table(root/"contrasts.csv")
    if len(blind)!=29952 or len(contrasts)!=312: raise ValueError("Incomplete primary cohort")
    seal=read(root/"blind.lock")
    for name,want in seal["files"].items():
        if hashlib.sha256((root/name).read_bytes()).hexdigest().upper()!=want: raise ValueError(name)
    index={(x["family"],x["slots"],x["scenario"],x["rep"],x["arm"]):x for x in blind}
    if len(index)!=len(blind):raise ValueError("Duplicate blind observation")
    for r in blind:
        if int(r["offered"])!=sum(int(r[k]) for k in ("accepted","blocked","in_flight")):
            raise ValueError("Unaccounted pressure")
    native=defaultdict(list)
    for r in blind:
        if r["arm"]=="coupled":native[r["family"],r["slots"]].append(r)
    ambiguous=0;comparable=0
    for rows in native.values():
        by_input=defaultdict(list)
        for r in rows:by_input[r["scenario"]].append(r)
        for group in by_input.values():
            for i,a in enumerate(group):
                for b in group[i+1:]:
                    if a["publication"]==b["publication"]:
                        comparable+=1;ambiguous+=a["ink"]!=b["ink"]
    for c in contrasts:
        vals=defaultdict(list)
        for a in native[c["family"],c["slots"]]:
            if c["contrast"]=="wrong-reference":d=float(a["matched_reference"])-float(a["wrong_reference"])
            else:
                b=index[a["family"],a["slots"],a["scenario"],a["rep"],c["contrast"]]
                d=float(a["broad"])-float(b["broad"])
            vals[a["parent"]].append(d)
        means=[statistics.mean(v) for v in vals.values()]
        margin=float(c["margin"])
        up=sum(x>=margin for x in means);down=sum(x<=-margin for x in means);within=len(means)-up-down
        if c["predicted"]=="equivalent":
            verdict="supported" if within>=8 else "contrary" if max(up,down)>=8 else "inconclusive"
        else:verdict="supported" if up>=8 else "contrary" if down>=8 else "inconclusive"
        if verdict!=c["status"] or abs(statistics.mean(means)-float(c["mean_delta"]))>1e-9:
            raise ValueError("Registered grouped comparison differs")
    pnl=table(root/"panel-use.csv")
    for p in pnl:
        w=[int(p[k]) for k in read(root/"design/settings.json")["judges"]]
        if sum(w)!=16 or not 2<=sum(v>0 for v in w)<=4:raise ValueError("Panel allocation mismatch")
    cert=dict(cases=len(blind),verdicts=len(contrasts),verified_panels=len(pnl),
              equal_publication_pairs=comparable,different_ink_among_them=ambiguous,
              scope="Pairs of the four coupled winners on identical physical inputs, within each named family.",
              published="The same byte can accompany different visible material. This is a lower-resolution observation.")
    (root/"verification.json").write_text(json.dumps(cert,indent=2),encoding="utf-8")
    return cert


if __name__=="__main__":
    print(verify(Path(sys.argv[1])))
