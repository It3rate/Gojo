"""Teacher-free replay and independent audit of within-bay versus whole-course simplicity."""
from pathlib import Path
from collections import defaultdict
import csv, json, math, hashlib, sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'OrnamentPathRefinement'))
from AuditWinding import native_traces

def examine(t):
    cells,pitch=t['cells'],t['pitch']; owners=defaultdict(list); raw_lengths=0; support=set()
    for e in t['edges']:
        a,b=[(t['sites'][i]['x'],t['sites'][i]['y']) for i in (e['from'],e['to'])]
        n=max(abs(a[0]-b[0]),abs(a[1]-b[1]))
        dx=(b[0]-a[0])//n;dy=(b[1]-a[1])//n; weight=math.hypot(dx,dy)
        raw_lengths+=n*weight
        for i in range(n):
            p=(a[0]+dx*i,a[1]+dy*i);q=(p[0]+dx,p[1]+dy);support.add(tuple(sorted((p,q))))
        owners[e['owner']].append(e)
    unique=sum(math.dist(p,q) for p,q in support);courses=[]
    def xy(i): s=t['sites'][i];return s['x'],s['y']
    def scale(n):return min(1,n/(.2*pitch)) if n>=.06*pitch else 0.
    for owner,edges in owners.items():
        runs=[]; seq=[]
        for e in edges:
            if seq and (seq[-1]['cell']!=e['cell'] or seq[-1]['to']!=e['from'] or (seq[-1]['direction']-e['direction'])%8==4):
                runs.append(seq);seq=[]
            if e['direction']%2:
                if seq:runs.append(seq);seq=[]
            else:seq.append(e)
        if seq:runs.append(seq)
        for run in runs:
            legs=[]
            for e in run:
                if legs and legs[-1][0]==e['direction']:legs[-1]=(e['direction'],legs[-1][1],e['to'])
                else:legs.append((e['direction'],e['from'],e['to']))
                for i in range(len(legs)-4):
                    piece=legs[i:];verts=[xy(piece[0][1])]+[xy(l[2]) for l in piece]; counts=[0]*8;min_length=10**9; boxes=[]
                    for j,(a,b) in enumerate(zip(verts,verts[1:])):
                        n=max(abs(a[0]-b[0]),abs(a[1]-b[1]));counts[piece[j][0]]+=n;min_length=min(min_length,n)
                        boxes.append((min(a[0],b[0]),max(a[0],b[0]),min(a[1],b[1]),max(a[1],b[1])))
                    gaps=[max(0,a[0]-b[1],b[0]-a[1],a[2]-b[3],b[2]-a[3]) for j,a in enumerate(boxes) for b in boxes[:max(0,j-1)]]
                    east,north,west,south=counts[::2]
                    clearance=min(1,max(0,(min(gaps)/pitch-.06)/.12))
                    val=scale(min(min_length,east,west,north,south))*scale(abs(east-west))*(.5+.5/(1+abs(north-south)/pitch))*clearance
                    if val>0:courses.append((owner,e['cell'],piece[0][1],piece[-1][2],val))
    best=[0.]*cells;strong=[False]*cells;through=[False]*cells; per_owner=defaultdict(set)
    for o,c,start,end,v in courses:
        joined=any(o==oo and vv>=.4 and ((cc==c-1 and endpoint==start and xy(start)[0]==c*pitch) or
                    (cc==c+1 and st==end and xy(end)[0]==(c+1)*pitch)) for oo,cc,st,endpoint,vv in courses)
        if v>=.4:strong[c]=True;through[c]|=joined;per_owner[o].add(c)
        best[c]=max(best[c],v*(1 if joined else .45))
    globally_simple=False;actual_bands=0
    for o,edges in owners.items():
        if len(per_owner[o])!=cells:continue
        sequences=[];seq=[]
        for e in edges:
            if seq and seq[-1]['to']!=e['from']:sequences.append(seq);seq=[]
            seq.append(e)
        if seq:sequences.append(seq)
        for seq in sequences:
            if len({e['cell'] for e in seq})!=cells:continue
            if any(e['direction']%2 for e in seq):continue
            if any(xy(e['from'])[0]!=e['cell']*pitch for a,e in zip(seq,seq[1:]) if a['cell']!=e['cell']):continue
            # Require a strong interval inside this actual run in every cell;
            # a different disconnected appearance of the same owner does not suffice.
            site_edges={(e['cell'],e['from']) for e in seq};end_edges={(e['cell'],e['to']) for e in seq}
            if len({c for own,c,s,z,val in courses if own==o and val>=.4 and (c,s) in site_edges and (c,z) in end_edges})!=cells:continue
            actual_bands+=1;seen={xy(seq[0]['from'])};repeat=False
            for e in seq:
                a,b=xy(e['from']),xy(e['to']);n=max(abs(a[0]-b[0]),abs(a[1]-b[1]))
                for k in range(1,n+1):
                    p=(a[0]+(b[0]-a[0])//n*k,a[1]+(b[1]-a[1])//n*k)
                    if p in seen: repeat=True
                    seen.add(p)
            if not repeat:globally_simple=True
    return sum(best)/cells*(unique/raw_lengths if raw_lengths else 0),sum(strong)/cells,sum(through)/cells,actual_bands,globally_simple

def main(run,kernel):
    cohort=json.loads((run/'cohort.json').read_text(encoding='utf-8-sig'))
    scenes={s['name']:s['input'] for s in cohort['final']}
    seal=json.loads((run/'seal.json').read_text(encoding='utf-8-sig'))
    if hashlib.sha256(kernel.read_bytes()).hexdigest().upper()!=seal['kernel']:raise ValueError('Wrong kernel')
    with (run/'final/cases.csv').open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
    jobs=[{'hex':r['hex'],'input':scenes[r['scene']]} for r in rows]
    traces=native_traces(jobs,kernel)
    if len(traces)!=len(rows):raise ValueError('Replay count mismatch.')
    target=run/'verification';target.mkdir(exist_ok=True)
    summary=defaultdict(lambda:{'cases':0,'whole_running_course':0,'whole_simple_course':0})
    with (target/'whole-course.csv').open('w',encoding='utf-8',newline='') as f:
        writer=csv.writer(f);writer.writerow(['arm','rep','scene','open_score','strong_fraction','through_fraction','spanning_courses','one_simple_spanning_course'])
        for r,t in zip(rows,traces):
            score,strong,through,spans,simple=examine(t)
            if len(t['edges'])!=int(r['edges']) or any(abs(v-float(r[k]))>1e-8 for v,k in zip((score,strong,through),('enhanced','strong','through'))):
                raise ValueError((r,score,strong,through))
            writer.writerow([r['arm'],r['rep'],r['scene'],score,strong,through,spans,int(simple)])
            m=summary[r['arm']];m['cases']+=1;m['whole_running_course']+=spans>0;m['whole_simple_course']+=simple
    (target/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print(json.dumps(summary))
if __name__=='__main__':main(Path(sys.argv[1]),Path(sys.argv[2]))
