"""Audit saved line-drawing exhibits. The evaluator and search engine are never invoked."""

import argparse
import csv
import hashlib
import json
from collections import defaultdict
from pathlib import Path

DIRS = ((1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1),(0,-1),(1,-1))


def occupied(segments):
    """Union collinear closed spans, invariant to subdivision, order and duplicate strokes."""
    lanes = defaultdict(list)
    for x1,y1,x2,y2 in segments:
        if x1 == x2:
            lane, a, b = ('v',x1),y1,y2
        else:
            slope = (y2-y1)//(x2-x1)
            lane, a, b = (slope,y1-slope*x1),x1,x2
        lanes[lane].append((min(a,b),max(a,b)))
    result = set()
    for lane, spans in lanes.items():
        low, high = sorted(spans)[0]
        for a,b in sorted(spans)[1:]:
            if a <= high:
                high = max(high,b)
            else:
                result.add((lane,low,high))
                low,high = a,b
        result.add((lane,low,high))
    return frozenset(result)


def cross(a,b,c):
    return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])


def audit(example):
    strokes = example['strokes']
    sites = [(p['x'],p['y']) for p in example['sites']]
    inp = example['realization']
    extents = [inp['a'],inp['b'],inp['a']+inp['b'],inp['pitch']]
    active = defaultdict(set)
    neighbors = defaultdict(set)
    incoming, outgoing, owners = defaultdict(set),defaultdict(set),defaultdict(set)
    geometric_segments = set()
    turns = set()
    draws = {(e['owner'],e['beat']):(e['from'],e['to']) for e in example['events'] if e['kind']=='draw'}
    if len(draws) != len(strokes):
        raise ValueError(example['id'] + ' has a repeated or missing draw opportunity')
    for s in strokes:
        u,v = s['a'],s['b']
        a,b = sites[u],sites[v]
        dx,dy = DIRS[s['direction']]
        length = extents[s['extent']]
        if ([*a,*b] != [s[k] for k in ('x1','y1','x2','y2')] or
                (b[0]-a[0],b[1]-a[1]) != (dx*length,dy*length) or
                draws[s['owner'],s['beat']] != (u,v) or (s['owner'],s['beat']) in turns):
            raise ValueError('Invalid drawn segment in '+example['id'])
        turns.add((s['owner'],s['beat']))
        active[a].add(u)
        active[b].add(v)
        neighbors[u].add(v)
        neighbors[v].add(u)
        owners[u].add(s['owner'])
        owners[v].add(s['owner'])
        outgoing[u].add((dx,dy))
        incoming[v].add((-dx,-dy))
        geometric_segments.add(tuple(sorted((a,b))))
    seen = set()
    components = 0
    for u in neighbors:
        if u in seen:
            continue
        components += 1
        stack = [u]
        seen.add(u)
        while stack:
            for v in neighbors[stack.pop()]:
                if v not in seen:
                    seen.add(v)
                    stack.append(v)
    lines = list(geometric_segments)
    crossings = 0
    for i,(a,b) in enumerate(lines):
        for c,d in lines[:i]:
            if cross(a,b,c)*cross(a,b,d)<0 and cross(c,d,a)*cross(c,d,b)<0:
                crossings += 1
    arms = {u:incoming[u]|outgoing[u] for u in neighbors}
    fork = {u for u in neighbors if len(arms[u])>=3 and len(outgoing[u])>=2}
    merge = {u for u in neighbors if len(arms[u])>=3 and len(incoming[u])>=2}
    edges = sum(map(len,neighbors.values()))//2
    counts = lambda name: sum(e['kind']==name for e in example['events'])
    old = bytes.fromhex(example['genome'])
    new = bytes.fromhex(example['finalGenome'])
    pos = 2
    frames = []
    for _ in range(old[1]):
        frames.append(pos+1)
        pos += 4+2*old[pos]
    changed = {i for i,(a,b) in enumerate(zip(old,new)) if a!=b}
    unwitnessed = set(changed) - set(frames)
    if len(old)!=len(new) or unwitnessed or any((old[i]^new[i])!=16 for i in changed):
        raise ValueError('Unexpected change outside a writable frame in '+example['id'])
    state = list(old)
    for event in example['events']:
        if event['kind']=='switch':
            addr = frames[event['owner']]
            if state[addr]!=event['beforeFrame'] or (state[addr]^16)!=event['afterFrame']:
                raise ValueError('Unaccounted switch at '+example['id'])
            state[addr] = event['afterFrame']
    if bytes(state)!=new:
        raise ValueError('Final program differs from its write history in '+example['id'])
    geometry = occupied((*a,*b) for a,b in geometric_segments)
    return dict(id=example['id'],brief=example['brief'],arm=example['arm'],scenario=example['scenario'],
                original=example['genome'],final=example['finalGenome'],score=example['utility'],
                draw_opportunities=len(turns),distinct_geometric_segments=len(lines),
                borrow_events=counts('branch'),target_attachment_events=counts('merge'),
                material_three_arm_sites=sum(len(d)>=3 for d in arms.values()),
                directed_fork_sites=len(fork),directed_merge_sites=len(merge),
                cross_owner_sites=sum(len(o)>=2 for o in owners.values()),
                identity_cycles=edges-len(neighbors)+components,
                projected_shared_positions=sum(len(ids)>=2 for ids in active.values()),
                strict_crossing_pairs=crossings,switch_events=counts('switch'),
                changed_frame_bytes=len(changed),geometry=geometry)


def run(exhibits,path):
    data = exhibits.read_bytes()
    results = [audit(e) for e in json.loads(data)['examples']]
    path.mkdir(parents=True,exist_ok=True)
    records = [{k:v for k,v in r.items() if k!='geometry'} for r in results]
    with (path/'history.csv').open('w',newline='',encoding='utf-8') as f:
        out = csv.DictWriter(f,fieldnames=list(records[0]))
        out.writeheader()
        out.writerows(records)
    by_id = {r['id']:r for r in results}
    controls = []
    for r in results:
        if r['arm']!='full' or not r['id'].endswith('-first'):
            continue
        row = {'id':r['id']}
        for arm in ('cut','frozen'):
            other = by_id[r['brief']+'-'+arm]
            if (r['original'],r['scenario'])!=(other['original'],other['scenario']):
                raise ValueError('Unmatched control')
            row[arm+'_changed_ink'] = r['geometry'] != other['geometry']
        controls.append(row)
    # Count each full finalist/exact scenario once. Curated exhibits do not
    # constitute a random sample of archive programs or of human evaluations.
    subset = {(r['brief'],r['original'],r['scenario']):r for r in results if r['arm']=='full'}.values()
    fields = ['borrow_events','target_attachment_events','material_three_arm_sites','directed_fork_sites',
              'directed_merge_sites','cross_owner_sites','identity_cycles','projected_shared_positions',
              'strict_crossing_pairs','switch_events','changed_frame_bytes']
    summary = dict(source_sha256=hashlib.sha256(data).hexdigest(),all_exhibits=len(results),
                   unique_full_exhibits=len(subset),controls=controls,
                   full_exhibits_with={k:sum(r[k]>0 for r in subset) for k in fields})
    (path/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    return summary


if __name__=='__main__':
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument('exhibits',type=Path)
    cli.add_argument('output',type=Path)
    args=cli.parse_args()
    print(json.dumps(run(args.exhibits,args.output),indent=2))
