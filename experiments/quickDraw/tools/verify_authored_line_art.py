"""Independent parity read of authored JSON versus restored native inspector JSON.

This audits the exported relation reads. Native archive deserialization is
performed by AnnotatedLineArtRunner; this script verifies the resulting reads
against the source independently and records hashes of each retained archive.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import string
import authored_line_art as art

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--root', type=Path, default=Path('D:/_quickDraw/gojo-line-art'),
                    help='Directory containing scenes.ndjson and native/inspector.ndjson')
ROOT = parser.parse_args().root.resolve()
REPO = Path(__file__).resolve().parents[3]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


checks = 0
def expect(actual, expected, context):
    global checks
    checks += 1
    if actual != expected:
        raise AssertionError(f'{context}: {actual!r} != {expected!r}')


def ray(edge, at_start):
    # A closed edge has two incidences at its shared endpoint. Selecting its
    # orientation by site equality would incorrectly count the start twice.
    controls = edge['controls'] if at_start else edge['controls'][::-1]
    for point in controls[1:]:
        if point != controls[0]:
            return [point[d]-controls[0][d] for d in (0, 1)]
    return [0, 0]


def site_rays(edges, selected_sites):
    return [ray(edge, endpoint == 'from') for edge in edges
            for endpoint in ('from', 'to') if edge[endpoint] in selected_sites]


def joint(rays):
    if not rays:
        return 'isolated'
    if len(rays) == 1:
        return 'endpoint'
    if any(r == [0, 0] for r in rays):
        return 'unresolved'
    # Two distinct incident edges leaving in the same direction do not expose
    # distinct visual branches. The letter atlas cannot resolve that overlap;
    # keep it unresolved rather than certifying a T/Y/X/K by edge count alone.
    if any(a[0]*b[1] == a[1]*b[0] and sum(x*y for x,y in zip(a,b)) > 0
           for i,a in enumerate(rays) for b in rays[i+1:]):
        return 'unresolved'
    opposed = sum(a[0]*b[1] == a[1]*b[0] and sum(x*y for x,y in zip(a,b)) < 0
                  for i,a in enumerate(rays) for b in rays[i+1:])
    return {(2,1):'I',(2,0):'L',(3,1):'T',(3,0):'Y',(4,2):'X',(4,1):'K'}.get((len(rays),opposed),'unresolved')


def line_contains(edge, point):
    a,b=edge['controls'][0],edge['controls'][-1]
    if len(edge['controls']) != 2:
        raise AssertionError('Audit only promises independent incidence for the authored line-crossing fixture')
    return ((point[0]-a[0])*(b[1]-a[1]) == (point[1]-a[1])*(b[0]-a[0])
            and all(min(a[d],b[d]) <= point[d] <= max(a[d],b[d]) for d in (0,1)))


source_path, native_path = ROOT/'scenes.ndjson', ROOT/'native/inspector.ndjson'
source = [json.loads(line) for line in source_path.read_text(encoding='utf-8').splitlines()]
native = [json.loads(line) for line in native_path.read_text(encoding='utf-8').splitlines()]
expect(source, art.church_scenes()+art.diagnostics(), 'Current source matches latest generator exactly')
expect([s['id'] for s in native], [s['id'] for s in source], 'Scene order and inventory')
summaries=[]
for src,out in zip(source,native):
    before=checks
    sid=src['id']; data=out['analysis']
    art.validate(src)
    expect(out['nativeRestoreVerified'],True,f'{sid} native restoration status')
    expect(out['viewBox'],src['viewBox'],f'{sid} viewport')
    expect(out['coordinateQuantum'],src['coordinateQuantum'],f'{sid} coordinate calibration')
    maps={scope:{r['id']:f'{prefix}{i}' for i,r in enumerate(src[scope])}
          for scope,prefix in [('sites','s'),('edges','e'),('strokes','p'),('objects','o'),('groups','g'),('regions','r')]}
    sites={s['id']:s for s in src['sites']}; edges={e['id']:e for e in src['edges']}
    objects={e['id']:[maps['objects'][o['id']] for o in src['objects'] if e['id'] in o['edges']] for e in src['edges']}
    def incident(site):
        return [e for e in src['edges'] if site in (e['from'],e['to'])]
    def site_objects(site):
        return list(dict.fromkeys(o for e in incident(site) for o in objects[e['id']]))
    for scope in ('sites','edges','strokes','objects','groups','regions','correspondences','crossings','contacts'):
        expect(len(data[scope]),len(src[scope]),f'{sid} {scope} count')
    for index,(a,b) in enumerate(zip(src['sites'],data['sites'])):
        expect([b['id'],b['sourceIndex'],b['x'],b['y']],[maps['sites'][a['id']],index,a['x'],a['y']],f'{sid} site identity/coordinate')
        inc=incident(a['id'])
        expect(bool(inc),True,f'{sid} no orphan authored site')
        expect(b['incidentEdges'],[maps['edges'][e['id']] for e in inc],f'{sid} site incidence identity')
        rays=site_rays(inc, {a['id']})
        expect(b['degree'],len(rays),f'{sid} degree')
        expect(b['kind'],joint(rays),f'{sid} exact tangent classification')
        expect(b['objectIds'],site_objects(a['id']),f'{sid} incident object scopes')
    for index,(a,b) in enumerate(zip(src['edges'],data['edges'])):
        expect([b['id'],b['sourceIndex'],b['from'],b['to']],[maps['edges'][a['id']],index,maps['sites'][a['from']],maps['sites'][a['to']]],f'{sid} ordered topology')
        expect(b['controls'],a['controls'],f'{sid} all native curve controls')
        expect(b['objectIds'],objects[a['id']],f'{sid} edge object membership')
        expect(b['groupIds'],[maps['groups'][g['id']] for g in src['groups'] if a['id'] in g['edges']],f'{sid} edge group membership')
        expect(b['regionIds'],[maps['regions'][r['id']] for r in src['regions'] if any(x['edge']==a['id'] for x in r['boundary'])],f'{sid} edge region membership')
        p,q=a['controls'][0],a['controls'][-1]
        orientation='zero' if p==q else 'horizontal' if p[1]==q[1] else 'vertical' if p[0]==q[0] else 'oblique'
        expect(b['chordOrientation'],orientation,f'{sid} chord orientation')
        direction=next(([c[0]-p[0],c[1]-p[1]] for c in a['controls'][1:] if c != p), None)
        straight=direction is not None and all((c[0]-p[0])*direction[1]==(c[1]-p[1])*direction[0] for c in a['controls'])
        expect(b['kind'],'degenerate' if direction is None else 'straight' if straight else 'curved',f'{sid} trace read')
    for a,b in zip(src['strokes'],data['strokes']):
        expect(b['edges'],[maps['edges'][e] for e in a['edges']],f'{sid} construction order')
        continuous=all(edges[x]['to']==edges[y]['from'] for x,y in zip(a['edges'],a['edges'][1:]))
        expect(b['continuous'],continuous,f'{sid} construction continuity')
    for scope in ('objects','groups'):
        for a,b in zip(src[scope],data[scope]):
            expect(b['id'],maps[scope][a['id']],f'{sid} {scope} identity')
            expect(b['edges'],[maps['edges'][e] for e in a['edges']],f'{sid} {scope} membership')
            if scope=='groups':
                expect(b['role'],a['role'],f'{sid} template/detail role')
    for a,b in zip(src['regions'],data['regions']):
        expect(b['boundary'],[{'edge':maps['edges'][x['edge']],'forward':x['forward']} for x in a['boundary']],f'{sid} oriented region cycle')
        oriented=[]
        for reference in a['boundary']:
            edge=edges[reference['edge']]
            ends=[edge['from'],edge['to']]
            oriented.append(ends if reference['forward'] else ends[::-1])
        closed=bool(oriented) and all(x[1]==y[0] for x,y in zip(oriented,oriented[1:]+oriented[:1]))
        expect(b['closed'],closed,f'{sid} actual identity closure')
        expect(b['role'],'figure' if a['figure'] else 'void',f'{sid} authored figure/void role')
        expect(b['parent'],maps['regions'][a['parent']] if a.get('parent') else None,f'{sid} region parent')
    for a,b in zip(src['correspondences'],data['correspondences']):
        expect(b['kind'],'translation' if a['kind']=='repeat' else a['kind'],f'{sid} correspondence transform')
        for side in ('source','target'):
            expect(b[side],[maps['edges'][e] for e in a[side]],f'{sid} correspondence {side}')
        reverse=a.get('reversed',[False]*len(a['source']))
        expect(b['reversed'],reverse,f'{sid} correspondence orientation')
        expect(b['axis'],a.get('axis'),f'{sid} authored mirror basis')
        expect(b['translation'],a.get('translation'),f'{sid} authored translation')
        forward,backward={},{}
        for from_edge,to_edge,reversed_ in zip(a['source'],a['target'],reverse):
            first,second=edges[from_edge],edges[to_edge]
            if a['kind']=='mirror':
                x=a['axis'][0][0]
                transformed=[[2*x-p[0],p[1]] for p in first['controls']]
            else:
                dx,dy=a['translation']
                transformed=[[p[0]+dx,p[1]+dy] for p in first['controls']]
            expect(transformed,second['controls'][::-1] if reversed_ else second['controls'],f'{sid} independent control map')
            target_ends=[second['to'],second['from']] if reversed_ else [second['from'],second['to']]
            for x,y in zip([first['from'],first['to']],target_ends):
                expect(forward.setdefault(x,y),y,f'{sid} shared-source-site map consistency')
                expect(backward.setdefault(y,x),x,f'{sid} target-site map injectivity')
        expect([b['verified'],b['geometryMatches'],b['identityMatches']],[True,True,True],f'{sid} native correspondence claims')
    for a,b in zip(src['crossings'],data['crossings']):
        expect(b['edges'],[maps['edges'][e] for e in a['edges']],f'{sid} crossing participants')
        expect(b['site'],a['site'],f'{sid} crossing location')
        first,second=[edges[e] for e in a['edges']]
        expect(b['geometricContact'],line_contains(first,a['site']) and line_contains(second,a['site']),f'{sid} projected contact')
        shared={first['from'],first['to']} & {second['from'],second['to']}
        joined=any([sites[s]['x'],sites[s]['y']]==a['site'] for s in shared)
        expect(b['joined'],joined,f'{sid} crossing versus shared site')
        expect(b['over'],maps['edges'].get(a.get('over')),f'{sid} authored/open over-order')
    for a,b in zip(src['contacts'],data['contacts']):
        first,second=a['sites']
        expect(first!=second,True,f'{sid} independent contact sites')
        expect(b['sites'],[maps['sites'][first],maps['sites'][second]],f'{sid} contact participants')
        coincidence=[sites[first]['x'],sites[first]['y']]==[sites[second]['x'],sites[second]['y']]
        expect(b['coincident'],coincidence,f'{sid} contact coincidence')
        expect(b['sameObject'],bool(set(site_objects(first)) & set(site_objects(second))),f'{sid} contact object separation')
        inc=[e for e in src['edges'] if first in (e['from'],e['to']) or second in (e['from'],e['to'])]
        expect(b['incidentEdges'],[maps['edges'][e['id']] for e in inc],f'{sid} contact projected incidence')
        rays=site_rays(inc, {first,second})
        expect(b['kind'],joint(rays) if coincidence else 'unresolved',f'{sid} contact projection form without welding')
    archive=ROOT/'native'/out['archive']
    expect(archive.exists(),True,f'{sid} native archive retained')
    summaries.append({'id':sid,'checks':checks-before,'sites':len(src['sites']),'edges':len(src['edges']),
                      'regions':len(src['regions']),'correspondences':len(src['correspondences']),
                      'archive':out['archive'],'archiveSha256':sha(archive)})


def connected_components(edges):
    adjacency = {}
    for edge in edges:
        a, b = edge['from'], edge['to']
        adjacency.setdefault(a, set()).add(b)
        adjacency.setdefault(b, set()).add(a)
    unseen = set(adjacency)
    components = 0
    while unseen:
        components += 1
        frontier = [unseen.pop()]
        while frontier:
            for adjacent in adjacency[frontier.pop()] & unseen:
                unseen.remove(adjacent)
                frontier.append(adjacent)
    return len(adjacency), components


def binary_rank(rows):
    """Rank of boundary edge incidence over GF(2), not a geometric fill read."""
    pivots = {}
    for row in rows:
        while row:
            pivot = row.bit_length() - 1
            if pivot not in pivots:
                pivots[pivot] = row
                break
            row ^= pivots[pivot]
    return len(pivots)


def audit_alphabet():
    src = next((s for s in source if s['id'] == 'diagnostic-alphabet'), None)
    if src is None:
        return None
    before = checks
    out = next(s for s in native if s['id'] == src['id'])
    data = out['analysis']
    expect(len(data['objects']), 26, 'Alphabet has 26 authored object scopes')
    expect([o['label'] for o in data['objects']], list(string.ascii_uppercase),
           'Alphabet external display labels address A through Z')
    expect(len(data['groups']), 26, 'Alphabet has 26 construction groups')
    expect(all(len(e['objectIds']) == 1 for e in data['edges']), True,
           'Every alphabet edge belongs to exactly one letter')
    expect(all(len(s['objectIds']) == 1 for s in data['sites']), True,
           'No shared site identity merges distinct letters')
    expect(all(len(e['groupIds']) == 1 for e in data['edges']), True,
           'Every alphabet edge belongs to exactly one construction group')
    expected_cycles = {'A': 1, 'B': 2, 'D': 1, 'O': 1, 'P': 1, 'Q': 1, 'R': 1}
    reports = []
    covered_regions = set()
    for obj in data['objects']:
        letter = obj['label']
        ids = set(obj['edges'])
        edges = [edge for edge in data['edges'] if edge['id'] in ids]
        sites = [site for site in data['sites'] if obj['id'] in site['objectIds']]
        group_ids = {group for edge in edges for group in edge['groupIds']}
        expect(len(group_ids), 1, f'Alphabet {letter} owns one construction group')
        group = next(g for g in data['groups'] if g['id'] in group_ids)
        expect(set(group['edges']), ids, f'Alphabet {letter} group preserves its object scope')
        vertex_count, components = connected_components(edges)
        cycles = len(edges) - vertex_count + components
        expect(components, 1, f'Alphabet {letter} is one connected object')
        expect(vertex_count, len(sites), f'Alphabet {letter} site inventory')
        expect(cycles, expected_cycles.get(letter, 0), f'Alphabet {letter} independent graph cycles')
        regions = [r for r in data['regions']
                   if r['boundary'] and all(step['edge'] in ids for step in r['boundary'])]
        expect(len(regions), cycles, f'Alphabet {letter} declared counter inventory')
        expect(all(r['closed'] for r in regions), True,
               f'Alphabet {letter} counters close by endpoint identity')
        expect(all(r['role'] == 'void' for r in regions), True,
               f'Alphabet {letter} counter roles are explicit authored voids')
        edge_bits = {edge['id']: 1 << index for index, edge in enumerate(edges)}
        boundaries = []
        for region in regions:
            row = 0
            for step in region['boundary']:
                row ^= edge_bits[step['edge']]
            boundaries.append(row)
            covered_regions.add(region['id'])
        rank = binary_rank(boundaries)
        expect(rank, cycles, f'Alphabet {letter} counters form an independent cycle basis')
        reports.append({'letter': letter, 'object': obj['id'], 'sites': vertex_count,
                        'edges': len(edges), 'connectedComponents': components,
                        'cycleRank': cycles, 'declaredCounters': len(regions),
                        'counterBoundaryRank': rank,
                        'counterRegions': [r['id'] for r in regions],
                        'traceKinds': dict(sorted(Counter(e['kind'] for e in edges).items())),
                        'siteKinds': dict(sorted(Counter(s['kind'] for s in sites).items())),
                        'junctions': [{'site': s['id'], 'kind': s['kind'], 'degree': s['degree']}
                                      for s in sites if s['degree'] > 2]})
    expect(covered_regions, {r['id'] for r in data['regions']},
           'Every alphabet counter belongs wholly to one letter')
    # These are explicit expectations for this authored fixture. They are not
    # a recognizer mapping an arbitrary shape or class label to native topology.
    by_letter = {row['letter']: row for row in reports}
    for letter, kind, count in [('A', 'T', 2), ('B', 'K', 1), ('H', 'T', 2),
                                ('I', 'T', 2), ('K', 'K', 1), ('P', 'T', 1),
                                ('Q', 'X', 1), ('R', 'K', 1),
                                ('T', 'T', 1), ('X', 'X', 1), ('Y', 'Y', 1)]:
        expect(by_letter[letter]['siteKinds'].get(kind, 0), count,
               f'Alphabet {letter} exact {kind} junction count')
    expect(by_letter['O']['siteKinds'], {'I': 4},
           'O is a closed curve with four continuation seams, not an O joint')
    expect(any(s['kind'] == 'O' for s in data['sites']), False,
           'O never appears as a local degree-two classification')
    report = {'schema': 'gojo.alphabet-native-analysis.v1', 'passed': True,
              'scene': src['id'], 'checks': checks-before, 'objects': len(reports),
              'sites': len(data['sites']), 'edges': len(data['edges']),
              'strokes': len(data['strokes']), 'closedCounterRegions': len(data['regions']),
              'correspondences': len(data['correspondences']),
              'verifiedCorrespondences': sum(c['verified'] for c in data['correspondences']),
              'correspondenceKinds': dict(sorted(Counter(c['kind'] for c in data['correspondences']).items())),
              'independentCycles': sum(row['cycleRank'] for row in reports),
              'siteKinds': dict(sorted(Counter(s['kind'] for s in data['sites']).items())),
              'traceKinds': dict(sorted(Counter(e['kind'] for e in data['edges']).items())),
              'scopeSeparationVerified': True,
              'nativeRestoreVerified': out['nativeRestoreVerified'],
              'archive': out['archive'], 'archiveSha256': sha(ROOT/'native'/out['archive']),
              'sourceSha256': sha(source_path), 'nativeInspectorSha256': sha(native_path),
              'auditScriptSha256': sha(Path(__file__)), 'letters': reports,
              'limits': [
                  'Letter names address authored fixture objects; no alphabet recognition model is evaluated.',
                  'Joint labels are exact local tangent/incidence reads. K is the declared four-ray, one-opposed-pair display convention.',
                  'Closed counter cycles and void roles are explicit authored claims verified for identity closure and independent cycle participation; geometric interior, filled area, and typographic meaning are not inferred.',
                  'Construction order is authored, not human drawing order.'
              ]}
    (ROOT/'alphabet-analysis.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    return report


alphabet_report = audit_alphabet()
result={'schema':'gojo.authored-native-parity.v1','passed':True,'sceneCount':len(source),'checks':checks,
        'totals':{key:sum(len(s[key]) for s in source) for key in ('sites','edges','strokes','objects','groups','regions','correspondences','crossings','contacts')},
        'sourceSha256':sha(source_path),'nativeInspectorSha256':sha(native_path),
        'generatorSha256':sha(REPO/'experiments/quickDraw/tools/authored_line_art.py'),
        'auditScriptSha256':sha(Path(__file__)),
        'verified':['latest source equals generator','no orphan sites','all native controls and site identities','ordered construction steps',
                    'object and template/detail scopes','oriented identity-closed regions and parent/figure/void roles',
                    'all authored transform geometry plus shared-site consistency and injectivity','crossing versus actual join',
                    'declared distinct-site contacts without identity welding','all exported tangent/trace reads'],
        'limits':['Native archive deserialization is performed by the runner; this independent script checks the restored exported reads against source.',
                  'Region simple interiors, occupied fill, semantic object truth and mechanical attachment are not established by this parity audit.',
                  'This is authored construction order, not human drawing order.'],'scenes':summaries}
if alphabet_report is not None:
    result['alphabetAnalysis'] = {'file': 'alphabet-analysis.json', 'checks': alphabet_report['checks'],
                                  'objects': alphabet_report['objects'],
                                  'independentCycles': alphabet_report['independentCycles']}
    result['verified'].append('26 disjoint connected letter scopes; independent counter cycles and declared exact junction cases')
(ROOT/'verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'passed':True,'scenes':len(source),'checks':checks,'totals':result['totals'],'output':str(ROOT/'verification.json')}))
