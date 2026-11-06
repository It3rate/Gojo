using System.IO.Compression;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentDensityResponse;

internal static partial class Program
{
    private sealed record Outcome(Winner W,Setting S,string Mode,Triple T);
    private sealed record Hypothesis(string Name,string Direction,double Margin,string Left,string Right,string Mode,string Statistic);
    private static readonly Hypothesis[] Questions=[
        new("learn-response","higher",.08,"response","form","live","response"),
        new("answer-vs-quiet","higher",.10,"response","response","quiet","response"),
        new("downward-route","higher",.06,"response","response","cut-down","response"),
        new("form-survives","equivalent",.035,"response","form","live","form")];
    private static readonly string[] Modes=["live","quiet","fixed","cut-down","misrouted","turn-lane","direct"];
    private static void Assay(string dir,JsonElement doc,string release)
    {
        var locked=CheckWinners(dir);
        if(!File.ReadAllText(release).Contains(Hash(Path.Combine(dir,"finalists.json")),StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Common reserve gate does not bind these finalists.");
        string dest=Path.Combine(dir,"reserve"); if(Directory.Exists(dest))throw new IOException("Reserve already opened.");
        var bank=Settings(doc,"reserve");
        var defaults=Families.Select(f=>new Winner(f,-1,"default",Convert.ToHexString(EngineLibrary.Create(f)),
            Convert.ToHexString(TensionMachine.DefaultPolicy()),0,0,0));
        var jobs=(from w in locked.Concat(defaults) from m in w.Arm=="default"?["live"]:Modes from s in bank select (w,m,s)).ToArray();
        var data=new Outcome[jobs.Length];
        Parallel.For(0,jobs.Length,new ParallelOptions{MaxDegreeOfParallelism=6},i=>{
            var (w,m,s)=jobs[i];
            var t=Trial(Convert.FromHexString(w.Path),Convert.FromHexString(w.Policy),w.Family,s,m,true,true);
            if(m is "quiet" or "cut-down" && (t.Plus.Material!=t.Minus.Material || t.Plus.Material!=t.Zero.Material))
                throw new InvalidDataException($"Control transmitted top-level demand in {m}");
            data[i]=new(w,s,m,t);
        });
        string tmp=dest+"-"+Guid.NewGuid().ToString("N")+".tmp";
        Directory.CreateDirectory(tmp);
        using(var writer=new StreamWriter(new GZipStream(File.Create(Path.Combine(tmp,"cases.csv.gz")),CompressionLevel.Fastest)))
        {
            writer.WriteLine("family,arm,rep,mode,case,parent,slots,path,policy,sign,unique_length,broad,course,reference,actuations,relief,ink,history,response,gain_pitch");
            foreach(var o in data)
            foreach(var (sign,r) in new[]{(1,o.T.Plus),(-1,o.T.Minus),(0,o.T.Zero)})
                writer.WriteLine($"{EngineLibrary.Names[o.W.Family]},{o.W.Arm},{o.W.Replicate},{o.Mode},{o.S.Key},{o.S.Parent},{o.S.Slots},{o.W.Path},{o.W.Policy},{sign},{Number(r.Length)},{Number(r.Broad)},{Number(r.Course)},{Number(r.Reference)},{r.Actions},{r.Relief},{r.Material},{r.History},{Number(o.T.Response)},{Number(o.T.Gain)}");
        }
        var comparisons=new List<object>();var groupRows=new List<string>{"family,test,parent,delta"};
        var keyed=data.ToDictionary(o=>(o.W.Family,o.W.Arm,o.W.Replicate,o.Mode,o.S.Key));
        foreach(int family in Families) foreach(var q in Questions)
        {
            var diffs=data.Where(o=>o.W.Family==family && o.W.Arm==q.Left && o.Mode=="live").GroupBy(o=>o.S.Parent)
                .Select(group=>{
                    double delta=group.Average(o=>{
                        var right=keyed[(family,q.Right,o.W.Replicate,q.Mode,o.S.Key)];
                        return q.Statistic=="response"?o.T.Response-right.T.Response:o.T.RequestedForm-right.T.RequestedForm;
                    });
                    groupRows.Add($"{EngineLibrary.Names[family]},{q.Name},{group.Key},{Number(delta)}");
                    return delta;
                }).ToArray();
            int up=diffs.Count(d=>d>=q.Margin),down=diffs.Count(d=>d<=-q.Margin),inside=diffs.Length-up-down;
            string status=q.Direction=="higher" ? (up>=8?"supported":down>=8?"contrary":"inconclusive") :
                (inside>=8?"supported":Math.Max(up,down)>=8?"contrary":"inconclusive");
            comparisons.Add(new{family=EngineLibrary.Names[family],q.Name,q.Direction,q.Margin,parents=diffs.Length,up,down,inside,mean=diffs.Average(),status});
        }
        Save(Path.Combine(tmp,"verdicts.json"),comparisons);
        File.WriteAllLines(Path.Combine(tmp,"groups.csv"),groupRows);
        var summary=data.GroupBy(o=>(o.W.Family,o.W.Arm,o.Mode)).Select(g=>new{family=EngineLibrary.Names[g.Key.Family],g.Key.Arm,g.Key.Mode,
            cases=g.Count(),response=g.Average(o=>o.T.Response),gain=g.Average(o=>o.T.Gain),broad=g.Average(o=>o.T.RequestedForm),
            changed=g.Count(o=>o.T.Plus.Material!=o.T.Minus.Material),positive=g.Count(o=>o.T.Gain>1e-8),
            negative=g.Count(o=>o.T.Gain< -1e-8), unchangedLength=g.Count(o=>Math.Abs(o.T.Gain)<1e-8)});
        Save(Path.Combine(tmp,"summary.json"),summary);
        var selected=new List<Outcome>();
        foreach(int family in Families)
        {
            int rep=locked.Where(w=>w.Family==family && w.Arm=="response").OrderByDescending(w=>w.Validation).ThenBy(w=>w.Replicate).First().Replicate;
            var live=data.Where(o=>o.W.Family==family && o.W.Replicate==rep && o.W.Arm=="response" && o.Mode=="live").ToArray();
            selected.Add(live.First(o=>o.S.Slots==5));
            selected.Add(live.MaxBy(o=>o.T.Gain)!);selected.Add(live.MinBy(o=>o.T.Gain)!);
        }
        Save(Path.Combine(tmp,"exhibits.json"),selected.Select((o,i)=>new{family=EngineLibrary.Names[o.W.Family],
            selection=new[]{"registered","largest gain","least gain"}[i%3],o.W.Replicate,o.S,o.W.Policy,o.W.Path,o.T.Gain,o.T.Response,
            traces=new[]{(1,o.T.Plus),(0,o.T.Zero),(-1,o.T.Minus)}.Select(pair=>{
                var path=Convert.FromHexString(o.W.Path);
                int half=PathMachine.Decode(path).Max(x=>x.Rows.Length)*o.S.Input.Cells/2;
                var pressure=pair.Item1==0?Array.Empty<PressureImpulse>():
                    new PressureImpulse[]{new(0,path[1]+1,0,3*pair.Item1),new(half,path[1]+1,0,3*pair.Item1)};
                var run=TensionMachine.Run(path,o.S.Input,o.S.Slots,Convert.FromHexString(o.W.Policy),FeedbackMode.Live,true,pressure);
                if (Digest(run.Trace)!=pair.Item2.History)throw new InvalidDataException("Witness changed the drawing");
                return new{sign=pair.Item1,run.Trace,timeline=run.Timeline,metrics=pair.Item2};
            })}).ToArray());
        Save(Path.Combine(tmp,"complete.json"),new{trials=data.Length,nativeDrawings=3*data.Length,gate=Hash(release),
            finalists=Hash(Path.Combine(dir,"finalists.json")),files=Directory.GetFiles(tmp).ToDictionary(f=>Path.GetFileName(f),Hash)});
        Directory.Move(tmp,dest);
        Console.WriteLine($"Recorded {data.Length} three-world cases.");
    }
}
