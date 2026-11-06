"""Re-execute every recorded reserve world in a fresh process, without a trainer."""
import argparse
import collections
import csv
import gzip
import hashlib
import json
from pathlib import Path
import statistics
import subprocess
import tempfile
from xml.sax.saxutils import escape


REPLAY=r"""
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;
CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
var json=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));
var scenes=doc.RootElement.GetProperty("reserve").EnumerateArray().ToDictionary(x=>x.GetProperty("key").GetString()!);
using var input=new StreamReader(new GZipStream(File.OpenRead(args[1]),CompressionMode.Decompress));
var header=input.ReadLine()!.Split(',');int total=0;
while(input.ReadLine() is {} line)
{
    var fields=line.Split(',');
    string F(string name)=>fields[Array.IndexOf(header,name)];
    var s=scenes[F("case")];
    var path=Convert.FromHexString(F("path"));var policy=Convert.FromHexString(F("policy"));
    var basis=JsonSerializer.Deserialize<PathInput>(s.GetProperty("input"),json);
    int sign=int.Parse(F("sign"));string mode=F("mode");
    int owner=mode=="direct"?0:path[1]+1,lane=mode=="turn-lane"?1:0;
    int half=PathMachine.Decode(path).Max(x=>x.Rows.Length)*basis.Cells/2;
    PressureImpulse[] impulses=sign==0?[]:[new(0,owner,lane,sign*3),new(half,owner,lane,sign*3)];
    var feedback=mode switch {"fixed"=>FeedbackMode.Fixed,"quiet"=>FeedbackMode.Quiet,"cut-down"=>FeedbackMode.CutDown,
        "misrouted"=>FeedbackMode.Misrouted,_=>FeedbackMode.Live};
    var run=TensionMachine.Run(path,basis,int.Parse(F("slots")),policy,feedback,false,impulses);
    var digest=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(run.Trace,json)));
    if(digest!=F("history"))throw new Exception("Trace mismatch at "+total);
    var read=TensionCritics.Measure(run.Trace,int.Parse(F("slots")),F("family"),true);
    if(Math.Abs(read.Broad-double.Parse(F("broad")))>1e-9 || Math.Abs(read.Reference-double.Parse(F("reference")))>1e-9 ||
       Math.Abs(read.Raw["connected-course"]-double.Parse(F("course")))>1e-9 ||
       run.Stats.Actuations!=int.Parse(F("actuations")) || run.Stats.Relief!=int.Parse(F("relief")))throw new Exception("Different read");
    var segments=new HashSet<(int X,int Y,int X2,int Y2)>();
    foreach(var e in run.Trace.Edges)
    {
        var a=run.Trace.Sites[e.From];var b=run.Trace.Sites[e.To];
        if(a.X>b.X || (a.X==b.X && a.Y>b.Y))(a,b)=(b,a);
        int dx=Math.Sign(b.X-a.X),dy=Math.Sign(b.Y-a.Y);
        for(int x=a.X,y=a.Y;x!=b.X || y!=b.Y;x+=dx,y+=dy)segments.Add((x,y,x+dx,y+dy));
    }
    double length=segments.Sum(p=>p.X!=p.X2 && p.Y!=p.Y2 ? Math.Sqrt(2) : 1);
    if(Math.Abs(length-double.Parse(F("unique_length")))>1e-8)throw new Exception("Unique length mismatch");
    total++;
}
Console.WriteLine($"Matched {total} native program/impulse traces, lengths, scores, actions and relief counts.");
"""


def verify(run,runtime):
    root=run/"reserve"
    complete=json.loads((root/"complete.json").read_bytes())
    for name,digest in complete["files"].items():
        assert hashlib.sha256((root/name).read_bytes()).hexdigest().upper()==digest
    with tempfile.TemporaryDirectory(prefix="GojoDensityReplay-") as d:
        temp=Path(d)
        dlls=["OrnamentTension","OrnamentPathCampaign","PrimitiveMeasure"]
        refs="".join('<Reference Include="'+n+'"><HintPath>'+escape(str((runtime/(n+'.dll')).resolve()))+'</HintPath></Reference>' for n in dlls)
        (temp/"Replay.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>'
            '<OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup>'+refs+'</ItemGroup></Project>')
        (temp/"Program.cs").write_text(REPLAY)
        out=subprocess.run(["dotnet","run","--project",str(temp/"Replay.csproj"),"-c","Release","--",
                            str((run/"settings.json").resolve()),str((root/"cases.csv.gz").resolve())],
                           capture_output=True,text=True,check=True)
        print(out.stdout.strip())
    with gzip.open(root/"cases.csv.gz","rt",encoding="utf-8-sig",newline="") as f:
        cases=list(csv.DictReader(f))
    assert len(cases)==complete["nativeDrawings"]==49248
    triples=collections.defaultdict(dict)
    for r in cases:triples[r["family"],r["arm"],r["rep"],r["mode"],r["case"]][int(r["sign"])]=r
    assert len(triples)==complete["trials"]==16416 and all(set(x)=={-1,0,1} for x in triples.values())
    scenes={x["key"]:x for x in json.loads((run/"settings.json").read_bytes())["reserve"]}
    for k,by_sign in triples.items():
        gain=(float(by_sign[1]["unique_length"])-float(by_sign[-1]["unique_length"]))/scenes[k[-1]]["input"]["pitch"]
        response=max(0,min(1,.5+.5*gain))
        assert all(abs(float(x["response"])-response)<1e-9 for x in by_sign.values())
        if k[3] in ("quiet","cut-down"):assert len({x["ink"] for x in by_sign.values()})==1
    verdicts=json.loads((root/"verdicts.json").read_bytes())
    for v in verdicts:
        pools=collections.defaultdict(list)
        for key,by_sign in triples.items():
            fam,arm,rep,mode,case=key
            if fam!=v["family"] or arm!="response" or mode!="live":continue
            other={"learn-response":("form","live"),"answer-vs-quiet":("response","quiet"),
                   "downward-route":("response","cut-down"),"form-survives":("form","live")}[v["name"]]
            opposite=triples[fam,other[0],rep,other[1],case]
            value=lambda w:statistics.mean(float(w[i]["broad"]) for i in (-1,1)) if v["name"]=="form-survives" else float(w[1]["response"])
            pools[scenes[case]["parent"]].append(value(by_sign)-value(opposite))
        parents=[statistics.mean(vals) for vals in pools.values()]
        assert len(parents)==12 and abs(statistics.mean(parents)-v["mean"])<1e-9
        pos=sum(n>=v["margin"] for n in parents);neg=sum(n<=-v["margin"] for n in parents);within=12-pos-neg
        expected=("supported" if within>=8 else "contrary" if max(pos,neg)>=8 else "inconclusive") if v["direction"]=="equivalent" else (
            "supported" if pos>=8 else "contrary" if neg>=8 else "inconclusive")
        assert (pos,neg,within,expected)==(v["up"],v["down"],v["inside"],v["status"])
    result={"native_traces":len(cases),"matched_triples":len(triples),"independent_verdicts":len(verdicts),
            "kernel_sha256":hashlib.sha256((runtime/"OrnamentTension.dll").read_bytes()).hexdigest().upper(),
            "raw_sha256":complete["files"]["cases.csv.gz"]}
    (run/"verification.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
    print(result)


if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("run",type=Path);p.add_argument("runtime",type=Path)
    args=p.parse_args();verify(args.run,args.runtime)
