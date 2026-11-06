"""Present the enrolled first final parent at one scale for programs and controls."""
import argparse,html,json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

PALETTE=['#144f41','#ac5533','#526c82']
LABEL={'old-blend':'Earlier return witness','open-blend':'Open course + clearance',
       'reference':'Supplied open course','box':'Supplied box and lead'}

def main(run):
    data=json.loads((run/'final/drawings.json').read_text(encoding='utf-8-sig'))
    def render(rep,file):
        examples=[r for r in data if r['rep']==rep or r['rep']<0]
        ordered=[next(r for r in examples if r['arm']==a) for a in LABEL]
        xys=[]
        for e in ordered:
            t=e['trace']; s=t['sites']; xys.extend(s[i] for z in t['edges'] for i in (z['from'],z['to']))
        scene=ordered[0]['scene']['input']
        xmin=min([0]+[n['x'] for n in xys]);xmax=max([scene['pitch']*scene['cells']]+[n['x'] for n in xys])
        ymin=min([0]+[n['y'] for n in xys]);ymax=max([0]+[n['y'] for n in xys])
        scale=min(550/max(1,xmax-xmin),139/max(1,ymax-ymin))
        W,H=1280,720
        im=Image.new('RGB',(W,H),'#faf8f0'); draw=ImageDraw.Draw(im)
        svg=[f'<svg xmlns="http://www.w3.org/2000/svg" role="img" viewBox="0 0 {W} {H}"><title>Prospective open-meander comparison</title><rect width="100%" height="100%" fill="#faf8f0"/>']
        def text(x,y,msg,size,bold=False):
            try:font=ImageFont.truetype('C:/Windows/Fonts/segoeui'+('b' if bold else '')+'.ttf',size)
            except OSError:font=ImageFont.load_default(size=size)
            draw.text((x,y),msg,font=font,fill='#233c33')
            svg.append(f'<text x="{x}" y="{y+size}" font-size="{size}" font-family="sans-serif" font-weight="{700 if bold else 400}" fill="#233c33">{html.escape(msg)}</text>')
        text(30,15,'Does the returning line stay open?',35,True)
        text(30,65,f"First fresh final parent, ordinary view. Shared scale, actual drawing history. A={scene['a']}, B={scene['b']}, pitch={scene['pitch']}.",21)
        for k,r in enumerate(ordered):
            col=k%2;row=k//2;x=30+630*col;y=125+row*283
            text(x,y,LABEL[r['arm']],25,True)
            text(x,y+40,'Trained finalist  ·  replicate '+str(rep+1) if row==0 else 'Construction control  ·  excluded from search',17)
            t=r['trace'];sites=t['sites']
            def xy(i):
                p=sites[i];return(x+12+(p['x']-xmin)*scale,y+70+(ymax-p['y'])*scale)
            for e in t['edges']:
                a,b=xy(e['from']),xy(e['to']);color=PALETTE[e['owner']]
                draw.line([a,b],fill=color,width=3)
                svg.append(f'<path d="M{a[0]:.2f},{a[1]:.2f}L{b[0]:.2f},{b[1]:.2f}" stroke-width="3" stroke="{color}" fill="none" stroke-linecap="round"/>')
            v=r['scores']
            text(x,y+225,f"Original {v['original']:.2f}   Open {v['enhanced']:.2f}   Strong bays {v['strong']:.0%}   Joined {v['through']:.0%}",17)
        svg.append('</svg>');im.save(run/(file+'.png'));(run/(file+'.svg')).write_text(''.join(svg),encoding='utf-8')
    representative=next(e['rep'] for e in data if e['representative'] and e['rep']>=0)
    render(representative,'Compare')
    for r in range(4):render(r,f'Replicate-{r+1}')
    summary=json.loads((run/'final/summary.json').read_text(encoding='utf-8-sig'))
    trs=''.join('<tr>'+''.join('<td>'+html.escape(cell)+'</td>' for cell in [LABEL[r['arm']],str(r['cases']),f"{r['original']:.3f}",f"{r['enhanced']:.3f}",f"{r['strong']:.1%}",f"{r['through']:.1%}"])+'</tr>' for r in summary)
    links=' · '.join(f'<a href="Replicate-{r}.svg">Replicate {r}</a>' for r in range(1,5))
    page=f"""<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Can the meander stay open?</title><style>body{{background:#faf8f0;color:#233c33;max-width:1240px;margin:32px auto;padding:16px;font:18px/1.5 system-ui}}img{{max-width:100%;height:auto}}th,td{{text-align:left;padding:6px 10px}}a{{color:#126350}}</style><h1>Can the meander stay open?</h1><p>Earlier key winners could close a box before advancing. The fresh competition asks for a continuous line that goes out, returns in both axes, leaves clearance, and can hand off to the next actual bay. The lower row shows two supplied controls.</p><img src="Compare.svg" alt="Two trained finalists and two construction controls"><p>{links}</p><h2>Across the twelve fresh final parents</h2><table><tr><th>Arm</th><th>Cases</th><th>Original</th><th>Open utility</th><th>Strong bays</th><th>Joined bays</th></tr>{trs}</table><p><a href="final/verdicts.json">Prospective predictions</a> · <a href="final/groups.csv">Parent differences</a> · <a href="Protocol.md">Registered design</a></p></html>"""
    (run/'Gallery.html').write_text(page,encoding='utf-8')
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('run',type=Path);main(p.parse_args().run)
