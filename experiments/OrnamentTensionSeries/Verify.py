"""Check every recorded series decision, contrast, and one teacher-free cold replay."""
import collections
import csv
import gzip
import hashlib
import json
import math
from pathlib import Path
import shutil
import statistics
import subprocess
import sys
import tempfile

def check(root,runtime):
    root=root.resolve();stage=root/"reserve"
    load=lambda f:json.loads(f.read_text(encoding="utf-8-sig"))
    digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
    complete=load(stage/"complete.json")
    for name,want in complete["files"].items():
        if digest(stage/name)!=want:raise ValueError(name)
    champs=load(root/"finalists.json")["winners"]
    with (stage/"collections.csv").open(newline="",encoding="utf-8-sig") as f:col=list(csv.DictReader(f))
    names=list(dict.fromkeys(x["family"] for x in col))
    rules={(names[c["tray"]["family"]],str(c["tray"]["replicate"])): bytes.fromhex(c["rule"]) for c in champs}
    def key(x):return (x["family"],x["rep"],x["setting"],x["mode"])
    groups=collections.defaultdict(list)
    with gzip.open(stage/"members.csv.gz","rt",encoding="utf-8-sig",newline="") as f:
        for x in csv.DictReader(f):groups[key(x)].append(x)
    index={key(x):x for x in col}
    if len(groups)!=len(index) or len(col)!=11520:raise ValueError('Unexpected series grid')
    signatures_changed=0
    native={}
    for k,steps in groups.items():
        family,rep,setting,mode=k
        assert len(steps)==8
        mask,select,admit,outlet=rules[family,rep]
        memory=[];side=int(rep)%2;charge=0;echo=False;excess=0
        for i,s in enumerate(steps):
            assert i==int(s["member"]) and echo==(s["echo"]=="True") and charge==int(s["before"])
            assert excess==int(s["overflow"])
            toggle=i>0 and (select >> (int(charge>0)+2*(i%2)))&1
            if mode=="Clock" and i>0 or mode in ("Live","TrayOnly") and toggle:side^=1
            assert side==int(s["tray"])
            ticket=i>0 and charge!=0 and (admit>>(int(echo)+2*(i%2)))&1
            spent=0
            if mode in ("Live","PulseOnly") and ticket:spent=int(math.copysign(min(abs(charge),1+(outlet>>3)),charge))
            assert int(s["spent"])==spent and int(s["pulse"])==(spent if not(outlet&4) else -spent)
            charge-=spent
            assert charge==int(s["after"])
            observed=int(s["publication"])&mask
            echo=observed in memory
            if memory:
                offer=1 if echo else -1
                settled=max(-3,min(3,charge+offer))
                excess=charge+offer-settled;charge=settled
            memory=(memory+[observed])[-2:]
        inks=[s["ink"] for s in steps]
        def close(a,b):return math.isclose(float(a),float(b),abs_tol=1e-10)
        assert close((len(set(inks))-1)/7,index[k]["exact"]) and len(set(inks))==int(index[k]["ink_outcomes"])
        for field in ("broad","reference","repeat"):
            assert close(statistics.mean(float(s[field]) for s in steps),index[k][field])
        for i,s in enumerate(steps):
            diagnostic=(family,rep,setting,i,s["tray"],s["lane"],s["pulse"])
            # Check separately repeated native jobs produce the identical ink and public byte.
            value=(s["ink"],s["publication"],s["broad"],s["reference"],s["repeat"],s["lower_acts"])
            if diagnostic in native: assert native[diagnostic]==value
            native[diagnostic]=value
            if mode=="Live" and (s["ink"],s["publication"])!=(
                    groups[(family,rep,setting,"TrayOnly")][i]["ink"],groups[(family,rep,setting,"TrayOnly")][i]["publication"]):
                signatures_changed+=1
    with (stage/"groups.csv").open() as f:published=list(csv.DictReader(f))
    with (stage/"verdicts.csv").open() as f:verdicts=list(csv.DictReader(f))
    plan={"wider-than-hold":("novelty","Hold"),"wider-than-clock":("novelty","Clock"),
          "pulses-add-variety":("novelty","TrayOnly"),"choices-add-variety":("novelty","PulseOnly"),
          "fewer-exact-copies":("exact","Hold"),"reference-preserved":("reference","Clock"),"repeat-preserved":("repeat","Clock")}
    for row in verdicts:
        metric,arm=plan[row["question"]];margin=float(row["margin"])
        parents=collections.defaultdict(list)
        for k,l in index.items():
            if k[-1]=="Live":
                parents[l["parent"]].append(float(l[metric])-float(index[k[:-1]+(arm,)][metric]))
        means={p:statistics.mean(a) for p,a in parents.items()}
        for p in [p for p in published if p["question"]==row["question"]]:
            assert close(means[p["parent"]],p["delta"])
        up=sum(x>=margin for x in means.values());dn=sum(x<=-margin for x in means.values())
        within=12-up-dn
        status=("supported" if up>=8 else "contrary" if dn>=8 else "inconclusive") if row['direction']=="higher" else (
            "supported" if within>=8 else "contrary" if max(up,dn)>=8 else "inconclusive")
        assert status==row["status"] and close(statistics.mean(means.values()),row["mean"])
    with tempfile.TemporaryDirectory() as temp:
        bare=Path(temp)
        for file in ["OrnamentTensionSeries.dll","OrnamentTensionSeries.deps.json","OrnamentTensionSeries.runtimeconfig.json",
                     "OrnamentTension.dll","OrnamentPathCampaign.dll","PrimitiveMeasure.dll"]:
            shutil.copy2(runtime.parent/file,bare/file)
        shutil.copy2(stage/"replay.json",bare/"input.json")
        subprocess.run(["dotnet","OrnamentTensionSeries.dll","play","input.json","actual.json"],cwd=bare,check=True)
        actual=digest(bare/"actual.json")
        assert actual==digest(stage/"expected.json")
    info=dict(families=len(names),collections=len(index),memberDecisions=8*len(index),distinctNativeInputs=len(native),groupDecisions=len(published),
              verdicts=len(verdicts),differentLiveVsTrayMemberReadbacks=signatures_changed,coldDrawingCount=8,
              coldSHA256=actual)
    (root/"verification.json").write_text(json.dumps(info,indent=2),encoding="utf-8")
    print(info)

if __name__=="__main__":check(Path(sys.argv[1]),Path(sys.argv[2]))
