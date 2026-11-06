"""Draw a matched retention case from native line traces on a common physical frame."""
from pathlib import Path
import sys
from Retention import load,run_jobs


def render(root):
    stage=root/'retention'
    w=next(w for w in load(stage/'finalists.json') if w['id']=='branched-spray/3')
    case=load(root/'wave1/design/design.json')['reserve'][0]
    jobs=[dict(id=arm,hex=w[arm],input=case['input'],mode=2 if arm=='frozen' else 0)
          for arm in ('fresh','carry','frozen')]
    draws=run_jobs(root/'runtime-review',jobs)
    coords=[(o['trace']['sites'][e['from']],o['trace']['sites'][e['to']])
            for o in draws for e in o['trace']['edges']]
    xs=[p['x'] for pair in coords for p in pair]
    ys=[p['y'] for pair in coords for p in pair]
    xmin,xmax=min(xs),max(xs)
    ymax=max(abs(min(ys)),abs(max(ys)),1)
    scale=min(680/max(xmax-xmin,1),52/ymax)
    svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 880 480" role="img" aria-labelledby="title">',
         '<title id="title">One learned machine, three frame conditions</title>',
         '<rect width="880" height="480" fill="#faf8f1"/>',
         '<g fill="#183f39" font-family="system-ui,sans-serif">',
         '<text x="28" y="40" font-size="25" font-weight="bold">The bits remember which side the path took</text>',
         f'<text x="28" y="70" font-size="16">Branched spray · first reserve realization · A={case["input"]["a"]}, B={case["input"]["b"]}, pitch={case["input"]["pitch"]}</text>']
    names={'fresh':'Original frame','carry':'After warmup','frozen':'Writes frozen'}
    for j,o in enumerate(draws):
        ybase=150+j*130
        svg += [f'<text x="28" y="{ybase+4}" font-size="16">{names[o["id"]]}</text>',
                f'<line x1="168" y1="{ybase}" x2="858" y2="{ybase}" stroke="#c9c6b8" stroke-dasharray="3 6"/>']
        for e in o['trace']['edges']:
            a,b=(o['trace']['sites'][e[k]] for k in ('from','to'))
            x1,x2=(172+(p['x']-xmin)*scale for p in (a,b))
            y1,y2=(ybase-p['y']*scale for p in (a,b))
            svg.append(f'<path d="M{x1:.3f} {y1:.3f}L{x2:.3f} {y2:.3f}" fill="none" stroke="#174e45" stroke-width="2.7" stroke-linecap="round"/>')
    (stage/'comparison.svg').write_text(''.join(svg)+'</g></svg>',encoding='utf-8')


if __name__=='__main__':
    render(Path(sys.argv[1]).resolve())
