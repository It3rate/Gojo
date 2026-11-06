"""Independent elementary-vector check of the exhibited traces and held-out verdicts."""
import csv
import gzip
import hashlib
import itertools
import json
import math
import statistics
import sys
from pathlib import Path

WORDS=[[(0,4),(4,0),(0,-2),(-2,0),(0,-2),(4,0)],
       [(0,6),(6,0),(0,-4),(-2,0),(0,2),(-2,0),(0,-4),(6,0)]]
PITCH=[6,8]

def atoms(lines):
    unique=set();total=0
    for x,y,x2,y2 in lines:
        dx=x2-x;dy=y2-y
        steps=max(abs(dx),abs(dy))
        sx,sy=dx//steps,dy//steps
        weight=math.hypot(sx,sy)
        total+=steps*weight
        for i in range(steps):
            a=x+i*sx,y+i*sy
            b=a[0]+sx,a[1]+sy
            unique.add((min(a,b),max(a,b)))
    return unique,total

def length(atoms):
    return sum(math.hypot(b[0]-a[0],b[1]-a[1]) for a,b in atoms)

def heading(x,y):
    return [(1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1),(0,-1),(1,-1)].index(
        ((x>0)-(x<0),(y>0)-(y<0)))

def fraction_matching(history,word):
    # Exhaust all target subsequences; no use of the runtime's dynamic-programming scorer.
    for n in range(len(word),0,-1):
        for sub in itertools.combinations(word,n):
            h=iter(history)
            if all(any(step==d for step in h) for d in sub):
                return 2*n/(len(word)+len(history))
    return 0.0

def check(row):
    t=row["trace"];fig=row["figure"]-1; q=PITCH[fig];p=t["pitch"]; cells=t["cells"]
    actual=[]
    for e in t["edges"]:
        a=t["sites"][e["from"]];b=t["sites"][e["to"]]
        actual.append((q*a["x"],q*a["y"],q*b["x"],q*b["y"]))
    target=[]; headings=[heading(x,y) for x,y in WORDS[fig]]
    for c in range(cells):
        x,y=q*c*p,0
        for dx,dy in WORDS[fig]:
            nx,ny=x+p*dx,y+p*dy;target.append((x,y,nx,ny));x,y=nx,ny
    ink,deposit=atoms(actual); ideal,_=atoms(target)
    iou=length(ink&ideal)/length(ink|ideal)
    clean=length(ink)/deposit if deposit else 0
    connected=[]
    for c in range(cells):
        runs=[]
        for owner in range(t['owners']):
            last=-1;run=[]
            for e in t['edges']:
                if e['cell']!=c or e['owner']!=owner: continue
                if e['from']!=last and run:runs.append(run);run=[]
                run.append(e['direction']);last=e['to']
            if run:runs.append(run)
        connected.append(max([fraction_matching(r,headings) for r in runs],default=0))
    order=statistics.mean(connected)
    seams=len({e['cell'] for e in t['events'] if e['kind']=='continue'})/(cells-1)
    expected=dict(ink=iou,order=order,course=seams,clean=clean,historical=.55*iou+.2*order+.15*seams+.1*clean)
    for k,v in expected.items():
        if abs(v-row['scores'][k])>1e-10:raise ValueError(f"{fig}/{row['arm']}/{k}: {v} != {row['scores'][k]}")
    if (ink==ideal)!=row['scores']['exactInk']:raise ValueError("Exact support read differs")

def verify(root):
    def load(p):return json.loads((root/p).read_bytes())
    for k,v in load('final/complete.json')['files'].items():
        assert hashlib.sha256((root/'final'/k).read_bytes()).hexdigest().upper()==v,k
    drawings=load('final/drawings.json')
    for d in drawings:check(d)
    with (root/'final/cases.csv').open(encoding='utf-8-sig',newline='') as f:
        cases=list(csv.DictReader(f))
    assert len(cases)==1344
    assert len({(r['figure'],r['arm'],r['rep'],r['parent'],r['slots'],r['mode']) for r in cases})==1344
    t=load('final/verdicts.json')
    for figure in (1,2):
        for p,recorded in enumerate(t[figure-1]['parentDeltas']):
            def avg(arm):
                return statistics.mean(float(r['historical']) for r in cases if r['figure']==str(figure) and
                                       r['parent']==str(p) and r['arm']==arm and r['mode']=='Live')
            assert abs(avg('printed-start')-avg('library-generic')-recorded)<1e-10
    counts=[]
    for path in (root/'histories').glob('*.gz'):
        with gzip.open(path,'rt',newline='') as f:counts.append(sum(1 for _ in csv.DictReader(f)))
    assert counts==[512]*12
    record=dict(cases=len(cases),elementary_vector_and_chronology_parity=len(drawings),
                paid_trials=sum(counts),cohort_parity=True)
    (root/'verification.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
    print(record)

if __name__=="__main__":verify(Path(sys.argv[1]))
