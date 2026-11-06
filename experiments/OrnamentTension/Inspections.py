"""Choose post-hoc diagnostic pairs by fixed rules and replay their saved bytes."""
import csv
import gzip
import hashlib
import json
import subprocess
import sys
from pathlib import Path


def main(run,runtime):
    with gzip.open(run/"blind.csv.gz","rt",newline="") as f:
        rows=list(csv.DictReader(f))
    seen={(r["family"],r["rep"],r["arm"],r["scenario"]):r for r in rows}
    live=[r for r in rows if r["arm"]=="coupled"]
    def control(r,a):
        return seen[r["family"],r["rep"],a,r["scenario"]]
    def diff(r,arm,metric="broad"):
        return float(r[metric])-float(control(r,arm)[metric])
    labels=[("Largest benefit from actuation",max(live,key=lambda r:diff(r,"disconnect")),"disconnect"),
            ("Largest loss from actuation",min(live,key=lambda r:diff(r,"disconnect")),"disconnect"),
            ("Largest effect of changing route strength",max(live,key=lambda r:abs(diff(r,"static"))),"static"),
            ("Largest benefit from the upper wire",max(live,key=lambda r:diff(r,"cut-down")),"cut-down")]
    settings=json.loads((run/"design/settings.json").read_bytes())
    inp={s["key"]:s["input"] for s in settings["blind"]}
    family=[s["key"] for s in settings["engines"]]
    wins=json.loads((run/"finalists.json").read_bytes())["winners"]
    recipes={(family[w["family"]],str(w["replicate"])):w["recipe"] for w in wins if w["arm"]=="coupled"}
    requests=[];pairs=[]
    for i,(title,r,arm) in enumerate(labels):
        items=[]
        for row,mode in [(control(r,arm),{"disconnect":"Quiet","static":"Fixed","cut-down":"CutDown"}[arm]),(r,"Live")]:
            uid=f"{i}-{mode}"
            data=recipes[r["family"],r["rep"]]
            requests.append(dict(id=uid,family=r["family"],path=data["path"],policy=data["policy"],input=inp[r["scenario"]],
                                 slots=int(r["slots"]),mode=mode))
            items.append(dict(id=uid,mode=mode,row=row))
        pairs.append(dict(title=title,items=items))
    out=run/"diagnostics";out.mkdir(exist_ok=True)
    requestfile=out/"requests.json";requestfile.write_text(json.dumps(requests),encoding="utf-8")
    output=out/"drawings.json"
    if not output.exists():
        subprocess.run(["dotnet",str(runtime),str(run/"seal.json"),str(requestfile),str(output)],check=True)
    drawings={r["id"]:r for r in json.loads(output.read_bytes())}
    for pair in pairs:
        for item in pair["items"]:
            d=drawings[item["id"]]; t=d["trace"];atoms=set()
            for e in t["edges"]:
                p,q=[(t["sites"][e[k]]["x"],t["sites"][e[k]]["y"]) for k in ("from","to")]
                if p>q: p,q=q,p
                dx=(q[0]>p[0])-(q[0]<p[0]);dy=(q[1]>p[1])-(q[1]<p[1])
                for j in range(max(abs(p[0]-q[0]),abs(p[1]-q[1]))):atoms.add((p[0]+j*dx,p[1]+j*dy,dx,dy))
            ink=hashlib.sha256(json.dumps(sorted(atoms),separators=(",",":")).encode()).hexdigest().upper()
            if ink!=item["row"]["ink"]:raise ValueError(item["id"])
            item["draw"]=d
    (out/"pairs.json").write_text(json.dumps(pairs),encoding="utf-8")


if __name__=="__main__":
    main(Path(sys.argv[1]),Path(sys.argv[2]))
