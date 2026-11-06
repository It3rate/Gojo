"""Post-hoc diagnostic: can a registered turn witness close a box before advancing?"""
from collections import defaultdict
import argparse, csv, hashlib, json, os, subprocess, tempfile
from pathlib import Path
from xml.sax.saxutils import escape

def native_traces(requests,kernel):
    with tempfile.TemporaryDirectory() as temporary:
        temp=Path(temporary)
        (temp/'Read.csproj').write_text(f"""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Reference Include="OrnamentPathCampaign"><HintPath>{escape(str(kernel.resolve()))}</HintPath></Reference><Reference Include="PrimitiveMeasure"><HintPath>{escape(str((kernel.parent/'PrimitiveMeasure.dll').resolve()))}</HintPath></Reference></ItemGroup></Project>""",encoding='utf-8')
        (temp/'Program.cs').write_text("""using Gojo.Experiments.OrnamentPathCampaign;
using System.Text.Json;
var opts=new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
var jobs=JsonSerializer.Deserialize<Job[]>(Console.In.ReadToEnd(),opts)!;
foreach(var j in jobs)
    Console.WriteLine(JsonSerializer.Serialize(PathMachine.Run(Convert.FromHexString(j.Hex),j.Input),opts));
record Job(string Hex,PathInput Input);""",encoding='utf-8')
        subprocess.run(['dotnet','build','--nologo','-c','Release','-v','quiet'],cwd=temp,check=True,stdout=subprocess.DEVNULL)
        p=subprocess.run(['dotnet',str(temp/'bin/Release/net10.0/Read.dll')],input=json.dumps(requests),
                         text=True,capture_output=True,check=True)
        return [json.loads(line) for line in p.stdout.splitlines()]

def interval(t):
    strengths=[[0.,0.] for _ in range(t['cells'])]
    owners=defaultdict(list)
    last={}
    def scale(n):
        ratio=n/t['pitch']
        return min(1,ratio/.2) if ratio>=.06 else 0.
    def xy(i): return t['sites'][i]['x'],t['sites'][i]['y']
    for e in t['edges']:
        c,o,d=e['cell'],e['owner'],e['direction']; course=owners[o]
        if d%2: course.clear(); last[o]=c;continue
        if course and (last[o]!=c or course[-1][2]!=e['from'] or (d-course[-1][0])%8==4):
            course.clear()
        last[o]=c
        if course and course[-1][0]==d: course[-1]=(d,course[-1][1],e['to'])
        else: course.append((d,e['from'],e['to']))
        for i in range(len(course)-4):
            distance=[0]*8
            points=set()
            path=course[i:]
            points.add(xy(path[0][1]))
            smallest=float('inf'); touches=False
            for direction,a,b in path:
                (x,y),(u,v)=xy(a),xy(b)
                n=max(abs(x-u),abs(y-v));distance[direction]+=n; smallest=min(smallest,n)
                dx=(u-x)//n;dy=(v-y)//n
                for _ in range(n):
                    x+=dx;y+=dy
                    if (x,y) in points:touches=True
                    points.add((x,y))
            east,north,west,south=distance[::2]
            value=scale(min(smallest,east,north,west,south))*scale(abs(east-west))*(.5+.5/(1+abs(north-south)/t['pitch']))
            strengths[c][0]=max(strengths[c][0],value)
            if not touches:strengths[c][1]=max(strengths[c][1],value)
    return tuple(sum(v[i]>=.4 for v in strengths)/len(strengths) for i in range(2))

def main(run,kernel):
    archive=json.loads((run/'seal.json').read_text(encoding='utf-8-sig'))
    if hashlib.sha256(kernel.read_bytes()).hexdigest().upper()!=archive['kernel']:
        raise ValueError('Original path runtime required')
    scenes={s['name']:s['input'] for s in json.loads((run/'wave-design.json').read_text(encoding='utf-8-sig'))['reserve']}
    with (run/'reserve/cases.csv').open(encoding='utf-8-sig',newline='') as f: rows=[r for r in csv.DictReader(f) if r['brief']=='returning-key']
    jobs=[{'hex':r['hex'],'input':scenes[r['scene']]} for r in rows]
    traces=native_traces(jobs,kernel)
    if len(traces)!=len(jobs): raise ValueError('Incomplete replay')
    out=run/'diagnostics';out.mkdir(exist_ok=True)
    stats=defaultdict(list)
    with (out/'winding.csv').open('w',encoding='utf-8',newline='') as f:
        writer=csv.writer(f);writer.writerow(['arm','rep','scene','registered_strong','simple_open_strong'])
        for r,t in zip(rows,traces):
            original,simple=interval(t)
            if abs(original-float(r['witness']))>1e-9: raise ValueError((r['arm'],r['scene'],original,r['witness']))
            writer.writerow([r['arm'],r['rep'],r['scene'],original,simple])
            stats[r['arm']].append((original,simple))
    data={arm:{'cases':len(values),'registered':sum(a for a,b in values)/len(values),'open':sum(b for a,b in values)/len(values),
               'with_any_open':sum(b>0 for a,b in values)} for arm,values in stats.items()}
    (out/'winding.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
    print(json.dumps(data))
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('run',type=Path);p.add_argument('kernel',type=Path)
    args=p.parse_args();main(args.run,args.kernel)
