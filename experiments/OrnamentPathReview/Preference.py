"""Train a small external observer, then test it on independently blinded drawings."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path
from BlindPairs import render

GROUPS=["craft","rhythm","field","symmetry","topology","seam"]

def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def save(path,obj):
    encoded=json.dumps(obj,indent=2)
    if path.exists() and path.read_text(encoding="utf-8")!=encoded:
        raise ValueError(f"Preserve existing record: {path}")
    path.write_text(encoded,encoding="utf-8")

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def dot(a,b):
    return sum(x*y for x,y in zip(a,b))

def fit(samples):
    # Solve the six-dimensional positive-definite ridge normal equations.
    n=len(GROUPS)
    matrix=[[sum(s["x"][i]*s["x"][j] for s in samples)+(.25 if i==j else 0)
             for j in range(n)]+[sum(s["x"][i]*s["target"] for s in samples)] for i in range(n)]
    for i in range(n):
        pivot=matrix[i][i]
        if pivot<=0: raise ValueError("Expected positive ridge pivot")
        matrix[i]=[x/pivot for x in matrix[i]]
        for j in range(n):
            if i!=j:
                factor=matrix[j][i]
                matrix[j]=[x-factor*y for x,y in zip(matrix[j],matrix[i])]
    return [row[-1] for row in matrix]

def samples(directory,design):
    keys=load(directory/"key/key.json")
    native={r["id"]:r for r in load(directory/"key/traces.json")}
    votes=[{r["pair"]:r["vote"] for r in load(directory/f"votes-{n}.json")} for n in (1,2)]
    out=[]
    for left,right in zip(keys[::2],keys[1::2]):
        pair=left["pair"]
        assert pair==right["pair"] and left["side"]=="L" and right["side"]=="R"
        readings=[]
        scores=[]
        for k in (left,right):
            r=native[pair+k["side"]]
            m=[0 if x is None else x for x in r["metrics"]]
            readings.append([sum(x for x,c in zip(m,design["criteria"]) if c["group"]==g)/
                             sum(c["group"]==g for c in design["criteria"]) for g in GROUPS])
            viable=len({e["direction"]%4 for e in r["trace"]["edges"]})>=2
            score=dot(m,design["weights"][k["brief"]]) if viable else 0
            if "utility" in k and abs(score-float(k["utility"]))>1e-7: raise ValueError("Replayed utility mismatch")
            scores.append(score)
        ratings=[{"L":1,"R":-1,"tie":0}[v[pair]] for v in votes]
        out.append(dict(pair=pair,brief=left["brief"],x=[l-r for l,r in zip(*readings)],ratings=ratings,
                        target=sum(ratings)/2,full=scores[0]-scores[1],arms=[left["arm"],right["arm"]]))
    return out

def decision(x):
    return 1 if x>1e-6 else -1 if x< -1e-6 else 0

def report(rows):
    unanimous=[s for s in rows if abs(s["target"])==1]
    return {"pairs":len(rows),"unanimous_direction":len(unanimous),
            "observer_agreement":sum(s["ratings"][0]==s["ratings"][1] for s in rows),
            "fixed_correct":sum(decision(s["full"])==s["target"] for s in unanimous),
            "learned_correct":sum(decision(s["model"])==s["target"] for s in unanimous),
            "full_arm_chosen":[sum(s["arms"][0 if s["ratings"][r]>0 else 1]=="full" for s in rows if s["ratings"][r]!=0)
                               for r in range(2)],
            "observer_ties":[sum(s["ratings"][r]==0 for s in rows) for r in range(2)],"rows":rows}

def prepare(run):
    root=run.parent/"preference"
    if (root/"visual-review/key/traces.json").exists(): raise ValueError("Reserve already open")
    design=load(run/"design/design.json")
    rows=samples(run/"visual-review",design)
    for s in rows:
        coefficients=fit([other for other in rows if other["brief"]!=s["brief"]])
        s["model"]=dot(coefficients,s["x"])
    save(root/"first-review.json",report(rows))
    winners={(w["brief"],w["replicate"],w["arm"]):w for w in load(run/"finalists.json")["winners"]}
    plan=[]
    for ri in range(4):
        for bi,b in enumerate(design["briefs"]):
            pair=f"Q{len(plan)//2+1:02d}"
            arms=["full","craft+field" if ri%2==0 else "seam+rhythm"]
            if hashlib.sha256(("path-reserve-920413/"+pair).encode()).digest()[0]&1: arms.reverse()
            s=design["reserve"][2*((3*bi+ri)%12)+1]
            for side,arm in zip("LR",arms):
                w=winners[b["name"],ri,arm]
                plan.append(dict(pair=pair,brief=b["name"],side=side,arm=arm,genome=w["hex"],
                                 scenario=s["name"],input=s["input"]))
    save(root/"lock.json",dict(protocol=digest(root/"Protocol.md"),finalists=digest(run/"finalists.json"),
         votes=[digest(run/"visual-review"/f"votes-{i}.json") for i in (1,2)],
         features=GROUPS,coefficients=fit(rows),plan=plan))

def reserve(run,runtime):
    root=run.parent/"preference"
    design=load(run/"design/design.json")
    lock=load(root/"lock.json")
    if lock["protocol"]!=digest(root/"Protocol.md") or lock["finalists"]!=digest(run/"finalists.json"):
        raise ValueError("Source of reserve registration changed")
    pairings=[(r["pair"],next(b for b in design["briefs"] if b["name"]==r["brief"])) for r in lock["plan"][::2]]
    directory=root/"visual-review"
    private=directory/"key"; public=directory/"panels"
    private.mkdir(parents=True,exist_ok=True); public.mkdir(exist_ok=True)
    save(private/"key.json",lock["plan"])
    save(private/"requests.json",[dict(id=k["pair"]+k["side"],hex=k["genome"],input=k["input"],mode=0) for k in lock["plan"]])
    if not (private/"traces.json").exists():
        subprocess.run(["dotnet",str(runtime),str(private/"requests.json"),str(private/"traces.json")],check=True)
    render(public,pairings,{r["id"]:r["trace"] for r in load(private/"traces.json")})

def score(run):
    root=run.parent/"preference"
    rows=samples(root/"visual-review",load(run/"design/design.json"))
    weights=load(root/"lock.json")["coefficients"]
    for s in rows: s["model"]=dot(weights,s["x"])
    result=report(rows)
    n,got,baseline=(result[k] for k in ("unanimous_direction","learned_correct","fixed_correct"))
    result["status"]=("inconclusive" if n<8 else "contrary" if got*2<n or got<=baseline-2
                      else "supported" if got*2>n and got>=baseline+2 else "inconclusive")
    save(root/"reserve-result.json",result)

if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action",choices=("prepare","reserve","score"))
    parser.add_argument("run",type=Path)
    parser.add_argument("runtime",type=Path,nargs="?")
    args=parser.parse_args()
    if args.action=="prepare": prepare(args.run)
    elif args.action=="reserve": reserve(args.run,args.runtime)
    else: score(args.run)
