"""Render actual recorded material edges at a common scale across each comparison."""
from pathlib import Path
import argparse, csv, html, json, math
from PIL import Image, ImageDraw, ImageFont

COLORS=['#18483d','#9d5836','#375a76']
LABEL={'returning-key':'Returning pottery key','ramified-spray':'Ramified spray',
       'baseline':'Original judges','blend':'Original + history','history':'History alone'}

def main(run):
    reserve=run/'reserve'
    data=json.loads((reserve/'exhibits.json').read_text(encoding='utf-8-sig'))
    rows=list(csv.DictReader((reserve/'summary.csv').open(encoding='utf-8-sig')))
    def image(name,selected):
        width,height=1290,630
        im=Image.new('RGB',(width,height),'#f9f7f1'); pen=ImageDraw.Draw(im)
        svg=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" role="img"><title>Recorded reserve path drawings</title><rect width="100%" height="100%" fill="#f9f7f1"/>']
        def label(x,y,txt,size=18,bold=False):
            try: f=ImageFont.truetype('C:/Windows/Fonts/segoeui'+('b' if bold else '')+'.ttf',size)
            except OSError: f=ImageFont.load_default(size=size)
            pen.text((x,y),txt,font=f,fill='#203d35')
            svg.append(f'<text x="{x}" y="{y+size}" fill="#203d35" font-family="sans-serif" font-size="{size}" font-weight="{700 if bold else 400}">{html.escape(txt)}</text>')
        label(26,12,'What changes when construction history selects the pen?',32,True)
        label(26,62,'Same reserved basis, actual executed bays; each row uses one geometric scale. Colors distinguish owners.',18)
        for i,brief in enumerate(('returning-key','ramified-spray')):
            subset=[e for e in selected if e['brief']==brief]
            dims=[]
            for e in subset:
                t=e['trace']; nodes=t['sites']
                used=[nodes[idx] for z in t['edges'] for idx in (z['from'],z['to'])]
                dims.append((min([0]+[n['x'] for n in used]),max([t['pitch']*t['cells']]+[n['x'] for n in used]),
                             min([0]+[n['y'] for n in used]),max([0]+[n['y'] for n in used])))
            xmin=min(b[0] for b in dims); xmax=max(b[1] for b in dims); ymin=min(b[2] for b in dims); ymax=max(b[3] for b in dims)
            scale=min(380/max(1,xmax-xmin),112/max(1,ymax-ymin))
            top=107+i*255
            label(26,top,LABEL[brief]+'  ·  replicate '+str(subset[0]['replicate']+1),23,True)
            for j,arm in enumerate(('baseline','blend','history')):
                e=next(e for e in subset if e['arm']==arm); t=e['trace']; read=e['read']
                x=26+j*423; label(x,top+36,LABEL[arm],17,True)
                def xy(idx):
                    p=t['sites'][idx]
                    return x+11+(p['x']-xmin)*scale,top+70+(ymax-p['y'])*scale
                for edge in t['edges']:
                    a,b=xy(edge['from']),xy(edge['to']); color=COLORS[edge['owner']]
                    pen.line([a,b],fill=color,width=3)
                    svg.append(f'<path d="M{a[0]:.2f},{a[1]:.2f} L{b[0]:.2f},{b[1]:.2f}" fill="none" stroke="{color}" stroke-width="2.6" stroke-linecap="round"/>')
                label(x,top+199,f"Original {read['original']:.2f}   History {read['history']:.2f}   Strong bays {read['witness']:.0%}",15)
        svg.append('</svg>')
        (run/(name+'.svg')).write_text(''.join(svg),encoding='utf-8')
        im.save(run/(name+'.png'))
    image('Compare',[e for e in data if e['representative']])
    for rep in range(4): image(f'Replicate-{rep+1}',[e for e in data if e['replicate']==rep])
    links=' '.join(f'<a href="Replicate-{r}.svg">Replicate {r}</a>' for r in range(1,5))
    table=''.join('<tr>'+''.join('<td>'+html.escape(part)+'</td>' for part in [LABEL[r['brief']],LABEL[r['arm']],f"{float(r['original']):.3f}",f"{float(r['history']):.3f}",f"{float(r['witness']):.1%}",f"{r['without_strong_witness']} / {r['cases']}"])+'</tr>' for r in rows)
    page=f"""<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Returning keys and through-stems</title><style>body{{background:#faf8f2;color:#203d35;font:18px/1.5 system-ui;margin:35px auto;max-width:1200px;padding:16px}}img{{max-width:100%;height:auto}}th,td{{padding:6px 10px;text-align:left}}a{{color:#18664d}}</style><h1>Returning keys and through-stems</h1><p>Each column trained under a different judge. The drawings come from independently selected programs on the same reserved input. The key requires a single pen to turn back in both axes. The spray requires a real side arm off a through-going stem.</p><img src="Compare.svg" alt="Six recorded drawings with matched geometry per row"><p>{links}</p><h2>Across all reserved inputs</h2><table><thead><tr><th>Brief</th><th>Trainer</th><th>Original</th><th>History</th><th>Strong bays</th><th>No strong bay</th></tr></thead><tbody>{table}</tbody></table><p><a href="reserve/groups.csv">Parent differences</a> · <a href="reserve/verdicts.json">Registered verdicts</a> · <a href="Protocol.md">Registered design</a></p></html>"""
    (run/'Gallery.html').write_text(page,encoding='utf-8')

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('run',type=Path)
    main(parser.parse_args().run)
