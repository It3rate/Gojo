"""Prospective retention assay using the frozen native path runtime and exact vector histories."""
import argparse
import base64
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

PROJECT=Path(__file__).resolve().parent
WARMUP=dict(a=2,b=1,pitch=6,cells=4,phase=1,seedBit=1)
ARMS=('fresh','carry','frozen','blind-flip')


def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def save(path,value):
    path.write_text(json.dumps(value,indent=2),encoding='utf-8')


def file_hash(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def run_jobs(runtime,jobs):
    with tempfile.TemporaryDirectory() as temp:
        source,output=Path(temp)/'input.json',Path(temp)/'draw.json'
        save(source,jobs)
        subprocess.run(['dotnet',str(runtime/'OrnamentPathReview.dll'),str(source),str(output)],check=True)
        return load(output)


def invert(hex_program):
    data=bytearray.fromhex(hex_program)
    pos=2
    for _ in range(data[1]):
        if data[pos+3] >> 2:
            data[pos+1] ^= 16
        pos+=4+2*data[pos]
    if pos!=len(data):
        raise ValueError('Invalid program')
    return data.hex().upper()


def inherited(trace):
    return base64.b64decode(trace['finalProgram']).hex().upper()


def freeze(root):
    stage=root/'retention'
    if stage.exists():
        raise FileExistsError('Choose a new output for a new study')
    stage.mkdir()
    runtime=root/'runtime-review'
    seals=load(root/'wave1/seal.json')
    binaries={p.name:file_hash(p) for p in sorted(runtime.glob('*.dll'))}
    if (binaries['OrnamentPathCampaign.dll']!=seals['host'] or
            binaries['PrimitiveMeasure.dll']!=seals['kernel']):
        raise ValueError('Incompatible primary runtime')
    shutil.copyfile(PROJECT/'Retention.md',stage/'Protocol.md')
    full=[w for w in load(root/'wave1/finalists.json')['winners'] if w['arm']=='full']
    if len(full)!=20 or len({(w['brief'],w['replicate']) for w in full})!=20:
        raise ValueError('Incomplete finalist cohort')
    jobs=[dict(id=w['brief']+'/'+str(w['replicate']),hex=w['hex'],input=WARMUP,mode=0) for w in full]
    warm=run_jobs(runtime,jobs)
    records=[]
    for w,request,record in zip(full,jobs,warm,strict=True):
        if request['id']!=record['id']:
            raise ValueError('Warmup order changed')
        trace=record['trace']
        fresh=w['hex']
        records.append(dict(id=request['id'],brief=w['brief'],replicate=w['replicate'],fresh=fresh,
                            carry=inherited(trace),frozen=fresh,**{'blind-flip':invert(fresh)},
                            warmSwitches=sum(e['kind']=='switch' for e in trace['events'])))
    save(stage/'finalists.json',records)
    save(stage/'lock.json',dict(protocol=file_hash(stage/'Protocol.md'),
         finalists=file_hash(stage/'finalists.json'),primaryFinalists=file_hash(root/'wave1/finalists.json'),
         primarySeal=file_hash(root/'wave1/seal.json'),runner=file_hash(Path(__file__)),
         runtime=binaries,warmup=WARMUP))
    print('Locked',file_hash(stage/'lock.json'),'warmup-changed',sum(w['fresh']!=w['carry'] for w in records))


def ink(trace):
    lanes={}
    for edge in trace['edges']:
        a,b=(trace['sites'][edge[k]] for k in ('from','to'))
        x1,y1,x2,y2=a['x'],a['y'],b['x'],b['y']
        if x1==x2:
            lane=('v',x1)
            span=(min(y1,y2),max(y1,y2))
        else:
            slope=(y2-y1)//(x2-x1)
            lane=(str(slope),y1-slope*x1)
            span=(min(x1,x2),max(x1,x2))
        lanes.setdefault(lane,[]).append(span)
    spans=[]
    for lane,intervals in sorted(lanes.items()):
        for a,b in sorted(intervals):
            if spans and spans[-1][0]==lane and a<=spans[-1][2]:
                spans[-1]=(lane,spans[-1][1],max(spans[-1][2],b))
            else:
                spans.append((lane,a,b))
    return hashlib.sha256(json.dumps(spans,separators=(',',':')).encode()).hexdigest()


def assay(root,gate):
    stage=root/'retention'
    if (stage/'complete.json').exists():
        raise FileExistsError('Finished assay is immutable')
    lock=load(stage/'lock.json')
    if file_hash(stage/'lock.json')!=gate or any([
        file_hash(stage/'Protocol.md')!=lock['protocol'],file_hash(stage/'finalists.json')!=lock['finalists'],
        file_hash(Path(__file__))!=lock['runner'],file_hash(root/'wave1/seal.json')!=lock['primarySeal'],
        file_hash(root/'wave1/finalists.json')!=lock['primaryFinalists']]):
        raise ValueError('Lock changed')
    runtime=root/'runtime-review'
    if any(file_hash(runtime/k)!=v for k,v in lock['runtime'].items()):
        raise ValueError('Execution base changed')
    basis=root/'wave1/design/design.json'
    if file_hash(basis)!=load(root/'wave1/seal.json')['files']['design.json']:
        raise ValueError('Parent design differs')
    design=load(basis)
    finalists=load(stage/'finalists.json')
    output=[]
    for case in design['reserve']:
        jobs=[dict(id=w['id']+'/'+arm,hex=w[arm],input=case['input'],mode=2 if arm=='frozen' else 0)
              for w in finalists for arm in ARMS]
        answers=run_jobs(runtime,jobs)
        for req,ans in zip(jobs,answers,strict=True):
            if req['id']!=ans['id']:
                raise ValueError('Execution order changed')
            brief,rep,arm=req['id'].split('/')
            trace=ans['trace']
            viable=len({e['direction']&3 for e in trace['edges']})>=2
            utility=sum((m or 0)*weight for m,weight in zip(ans['metrics'],design['weights'][brief],strict=True)) if viable else 0
            output.append(dict(scenario=case['name'],parent=case['parent'],brief=brief,replicate=int(rep),arm=arm,
                               start=req['hex'],end=inherited(trace),utility=utility,ink=ink(trace),
                               strokes=len(trace['edges']),switches=sum(e['kind']=='switch' for e in trace['events'])))
    with (stage/'reserve-cases.csv').open('w',newline='',encoding='utf-8') as f:
        writer=csv.DictWriter(f,fieldnames=list(output[0]))
        writer.writeheader()
        writer.writerows(output)
    keyed={}
    for r in output:
        key=(r['parent'],r['brief'],r['replicate'],r['scenario'])
        keyed.setdefault(key,{})[r['arm']]=r
    parents=sorted({k[0] for k in keyed})
    if len(output)!=1920 or len(keyed)!=480 or len(parents)!=12:
        raise ValueError('Incomplete reserve')
    changed={(w['brief'],w['replicate']) for w in finalists if w['fresh']!=w['carry']}
    distinct={(w['brief'],w['replicate']) for w in finalists if w['carry'] not in (w['fresh'],w['blind-flip'])}
    all_owners={(w['brief'],w['replicate']) for w in finalists}
    effects={}
    for b,r in sorted(changed):
        effects[b+'/'+str(r)]=sum(any(v['carry']['ink']!=v['fresh']['ink'] for k,v in keyed.items()
                                    if k[:3]==(p,b,r)) for p in parents)
    verdicts=[dict(id='R1',status='supported' if max(effects.values(),default=0)>=6 else 'failed',visibleParents=effects)]
    group_rows=[]
    for label,left,right,subset,equivalence in [
            ('R2','carry','fresh',all_owners,True),('R3','carry','blind-flip',distinct,False),
            ('R4','carry','frozen',changed,False),('diagnostic-changed','carry','fresh',changed,False),
            ('diagnostic-writes','fresh','frozen',all_owners,False)]:
        if not subset:
            verdicts.append(dict(id=label,status='unavailable',eligible=0))
            continue
        means=[]
        for p in parents:
            paired=[v[left]['utility']-v[right]['utility'] for k,v in keyed.items() if k[0]==p and k[1:3] in subset]
            mean=sum(paired)/len(paired)
            means.append(mean)
            group_rows.append(dict(id=label,parent=p,pairs=len(paired),meanDelta=mean))
        up=sum(m>=.02 for m in means)
        down=sum(m<=-.02 for m in means)
        within=sum(abs(m)<=.02 for m in means)
        if label.startswith('diagnostic'):
            status='descriptive'
        elif equivalence:
            status='supported' if within>=8 else 'contrary' if max(up,down)>=8 else 'inconclusive'
        else:
            status='supported' if up>=8 else 'contrary' if down>=8 else 'inconclusive'
        verdicts.append(dict(id=label,status=status,eligible=len(subset),positive=up,negative=down,within=within,
                             meanDelta=sum(means)/len(means)))
    if any(v[a]['ink']!=v[b]['ink'] or v[a]['end']!=v[b]['end'] or v[a]['utility']!=v[b]['utility']
           for v in keyed.values() for a,b in [('fresh','carry'),('carry','blind-flip')] if v[a]['start']==v[b]['start']):
        raise ValueError('Identical live inputs have different outcomes')
    with (stage/'reserve-parents.csv').open('w',newline='',encoding='utf-8') as f:
        writer=csv.DictWriter(f,fieldnames=list(group_rows[0]))
        writer.writeheader()
        writer.writerows(group_rows)
    save(stage/'verdicts.json',verdicts)
    save(stage/'complete.json',dict(lock=gate,cases=len(output),warmChanged=len(changed),distinctFromFlip=len(distinct),
                                  files={name:file_hash(stage/name) for name in ['reserve-cases.csv','reserve-parents.csv','verdicts.json']}))
    print(json.dumps(verdicts,indent=2))


if __name__=='__main__':
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument('phase',choices=['freeze','assay'])
    cli.add_argument('root',type=Path)
    cli.add_argument('--gate',help='authorized combined-gate retention lock hash')
    args=cli.parse_args()
    if args.phase=='freeze':
        freeze(args.root.resolve())
    elif args.gate:
        assay(args.root.resolve(),args.gate)
    else:
        cli.error('the assay requires its previously approved gate lock hash')
