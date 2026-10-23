"""Independent parity read of authored JSON versus restored native inspector JSON.

This audits the exported relation reads. Native archive deserialization is
performed by AnnotatedLineArtRunner; this script verifies the resulting reads
against the source independently and records hashes of each retained archive.
"""
import argparse
import hashlib
import json
from pathlib import Path
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


def ray(edge, site):
    controls = edge['controls'] if edge['from'] == site else edge['controls'][::-1]
    for point in controls[1:]:
        if point != controls[0]:
            return [point[d]-controls[0][d] for d in (0, 1)]
    return [0, 0]


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
        rays=[ray(e,a['id']) for e in inc]
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
        straight=all((c[0]-p[0])*(q[1]-p[1])==(c[1]-p[1])*(q[0]-p[0]) for c in a['controls'])
        expect(b['kind'],'straight' if straight else 'curved',f'{sid} trace read')
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
        rays=[ray(e,first if first in (e['from'],e['to']) else second) for e in inc]
        expect(b['kind'],joint(rays),f'{sid} contact projection form without welding')
    archive=ROOT/'native'/out['archive']
    expect(archive.exists(),True,f'{sid} native archive retained')
    summaries.append({'id':sid,'checks':checks-before,'sites':len(src['sites']),'edges':len(src['edges']),
                      'regions':len(src['regions']),'correspondences':len(src['correspondences']),
                      'archive':out['archive'],'archiveSha256':sha(archive)})

result={'schema':'gojo.authored-native-parity.v1','passed':True,'sceneCount':len(source),'checks':checks,
        'totals':{key:sum(len(s[key]) for s in source) for key in ('sites','edges','strokes','objects','groups','regions','correspondences','crossings','contacts')},
        'sourceSha256':sha(source_path),'nativeInspectorSha256':sha(native_path),
        'generatorSha256':sha(REPO/'experiments/quickDraw/tools/authored_line_art.py'),
        'auditScriptSha256':sha(Path(__file__)),
        'verified':['latest source equals generator','no orphan sites','all native controls and site identities','ordered construction steps',
                    'object and template/detail scopes','oriented identity-closed regions and parent/figure/void roles',
                    'all authored transform geometry plus shared-site consistency and injectivity','crossing versus actual join',
                    'distinct-site contact, object separation, projected T','all exported tangent/trace reads'],
        'limits':['Native archive deserialization is performed by the runner; this independent script checks the restored exported reads against source.',
                  'Region simple interiors, occupied fill, semantic object truth and mechanical attachment are not established by this parity audit.',
                  'This is authored construction order, not human drawing order.'],'scenes':summaries}
(ROOT/'verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'passed':True,'scenes':len(source),'checks':checks,'totals':result['totals'],'output':str(ROOT/'verification.json')}))
