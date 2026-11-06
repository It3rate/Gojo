"""Reproduce the retention table and inspect why a carried bit can change ink at equal utility."""
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path
import sys
import tempfile
import shutil

from Retention import ARMS,file_hash,load,save,run_jobs,ink,inherited


def check(root):
    stage=root/'retention'
    done=load(stage/'complete.json')
    if file_hash(stage/'lock.json')!=done['lock'] or any(file_hash(stage/k)!=v for k,v in done['files'].items()):
        raise ValueError('Reserve artifact changed')
    with (stage/'reserve-cases.csv').open(newline='') as f:
        rows=list(csv.DictReader(f))
    keyed={(r['scenario'],r['brief'],int(r['replicate']),r['arm']):r for r in rows}
    if len(keyed)!=len(rows):
        raise ValueError('Duplicate case')
    design=load(root/'wave1/design/design.json')
    finalists=load(stage/'finalists.json')
    horizons=defaultdict(Counter)
    translations=Counter()
    deltas=[]
    replayed=0
    with tempfile.TemporaryDirectory() as tmp:
        runtime=Path(tmp)
        for name in ('OrnamentPathReview.dll','OrnamentPathReview.deps.json','OrnamentPathReview.runtimeconfig.json',
                     'PrimitiveMeasure.dll','OrnamentPathCampaign.dll'):
            shutil.copyfile(root/'runtime-review'/name,runtime/name)
        for s in design['reserve']:
            jobs=[]
            for w in finalists:
                jobs.extend(dict(id=w['id']+'/'+a,hex=w[a],input=s['input'],mode=2 if a=='frozen' else 0) for a in ARMS)
            results=run_jobs(runtime,jobs)
            fresh_trace={}
            for job,out in zip(jobs,results,strict=True):
                brief,r,arm=job['id'].split('/')
                trace=out['trace']
                record=keyed[s['name'],brief,int(r),arm]
                viable=len({e['direction']&3 for e in trace['edges']})>=2
                score=sum((m or 0)*w for m,w in zip(out['metrics'],design['weights'][brief],strict=True)) if viable else 0
                assert out['id']==job['id'] and record['ink']==ink(trace) and float(record['utility'])==score
                assert record['start']==job['hex'] and record['end']==inherited(trace)
                assert len(trace['edges'])==int(record['strokes'])
                assert int(record['switches'])==sum(e['kind']=='switch' for e in trace['events'])
                replayed+=1
                if arm=='fresh':
                    fresh_trace[brief,r]=trace
                if arm=='carry':
                    h=horizons[s['input']['cells']]
                    h['pairs']+=1
                    before=keyed[s['name'],brief,int(r),'fresh']
                    delta=score-float(before['utility'])
                    deltas.append(delta)
                    h['utility_changed']+=abs(delta)>1e-12
                    if before['ink']!=record['ink']:
                        h['material_changed']+=1
                        # Search only a vertical reflection, with free vertical translation
                        # fixed by the two minimum/maximum ink extents.
                        old=fresh_trace[brief,r]
                        def ys(t):
                            return [t['sites'][e[p]]['y'] for e in t['edges'] for p in ('from','to')]
                        axis_twice=max(ys(old))+min(ys(trace))
                        mirrored=dict(old,sites=[dict(p,y=axis_twice-p['y']) for p in old['sites']])
                        if ink(mirrored)==record['ink']:
                            translations[str(axis_twice)]+=1
    report=dict(rawCases=replayed,cleanRuntimeFiles=5,carryMinusFreshRange=[min(deltas),max(deltas)],
                horizons={k:dict(v) for k,v in sorted(horizons.items())},reflectedMaterialPairsByTwiceAxis=dict(translations))
    save(stage/'verification.json',report)
    print(json.dumps(report,indent=2))


if __name__=='__main__':
    check(Path(sys.argv[1]).resolve())
