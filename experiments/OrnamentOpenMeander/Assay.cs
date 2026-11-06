using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentOpenMeander;

internal static partial class Program
{
    private sealed record Record(string Arm,int Rep,string Hex,Scene Scene,Scores Scores,PathTrace Trace);
    private static void Assay(string run,Cohort c,string release)
    {
        string final=Path.Combine(run,"final"),hash=Hash(Path.Combine(run,"finalists.json"));
        var lockData=JsonDocument.Parse(File.ReadAllText(Path.Combine(run,"lock.json"))).RootElement;
        if(lockData.GetProperty("finalists").GetString()!=hash||lockData.GetProperty("count").GetInt32()!=8||
            lockData.GetProperty("seal").GetString()!=Hash(Path.Combine(run,"seal.json"))||
            !File.ReadAllText(release).Contains(hash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Release must bind the 8 frozen winners.");
        Directory.CreateDirectory(final);if(File.Exists(Path.Combine(final,"complete.json")))throw new IOException("Final bank already read.");
        var winners=JsonSerializer.Deserialize<Champion[]>(File.ReadAllText(Path.Combine(run,"finalists.json")),Json)!;
        var cases=new List<Record>();
        foreach(var (arm,rep,hex) in winners.Select(w=>(w.Arm,w.Rep,w.Hex)).Concat(new[]{("reference",-1,Convert.ToHexString(Design.Reference())),("box",-1,Convert.ToHexString(Design.Box()))}))
        foreach(var s in c.Final)
        {
            var t=PathMachine.Run(Convert.FromHexString(hex),s.Input);
            cases.Add(new(arm,rep,hex,s,Measure(t,c.Weights),t));
        }
        int representative=winners.GroupBy(w=>w.Rep).OrderByDescending(g=>g.Average(w=>w.Validation.Goal(w.Arm))).ThenBy(g=>g.Key).First().Key;
        Save(Path.Combine(final,"drawings.json"),cases.Where(x=>x.Scene.Name==c.Final[0].Name).Select(x=>new{x.Arm,x.Rep,x.Hex,x.Scene,x.Scores,x.Trace,representative=x.Rep==representative||x.Rep<0}));
        using(var f=File.CreateText(Path.Combine(final,"cases.csv")))
        {
            f.WriteLine("arm,rep,hex,scene,parent,original,old,enhanced,strong,through,edges");
            foreach(var x in cases){var v=x.Scores;f.WriteLine($"{x.Arm},{x.Rep},{x.Hex},{x.Scene.Name},{x.Scene.Parent},{v.Original:R},{v.Prior:R},{v.Enhanced:R},{v.Strong:R},{v.Through:R},{x.Trace.Edges.Length}");}
        }
        var lookup=cases.Where(x=>x.Rep>=0).ToDictionary(x=>(x.Arm,x.Rep,x.Scene.Name));var groups=new List<string>{"read,parent,delta"};
        var verdicts=new List<object>();
        foreach(var (name,margin,eq) in new[]{("enhanced",.06,false),("strong",.10,false),("through",.10,false),("original",.06,true)})
        {
            double Value(Scores s)=>name switch{"enhanced"=>s.Enhanced,"strong"=>s.Strong,"through"=>s.Through,_=>s.Original};
            var ds=new List<double>();
            foreach(var parent in c.Final.GroupBy(x=>x.Parent))
            {
                double diff=parent.SelectMany(s=>Enumerable.Range(0,4).Select(r=>Value(lookup[("open-blend",r,s.Name)].Scores)-Value(lookup[("old-blend",r,s.Name)].Scores))).Average();
                ds.Add(diff);groups.Add($"{name},{parent.Key},{diff:R}");
            }
            int positive=ds.Count(n=>n>=margin),negative=ds.Count(n=>n<=-margin),within=ds.Count(n=>Math.Abs(n)<margin);
            string status=eq ? within>=8?"supported":Math.Max(positive,negative)>=8?"contrary":"inconclusive" : positive>=8?"supported":negative>=8?"contrary":"inconclusive";
            verdicts.Add(new{name,margin,relation=eq?"equivalent":"higher",positive,negative,within,mean=ds.Average(),status});
        }
        Save(Path.Combine(final,"verdicts.json"),verdicts);File.WriteAllLines(Path.Combine(final,"groups.csv"),groups);
        var summary=cases.GroupBy(x=>x.Arm).Select(g=>new{arm=g.Key,cases=g.Count(),original=g.Average(x=>x.Scores.Original),old=g.Average(x=>x.Scores.Prior),
            enhanced=g.Average(x=>x.Scores.Enhanced),strong=g.Average(x=>x.Scores.Strong),through=g.Average(x=>x.Scores.Through),
            anyOpen=g.Count(x=>x.Scores.Strong>0),anyThrough=g.Count(x=>x.Scores.Through>0)}).ToArray();
        Save(Path.Combine(final,"summary.json"),summary);
        string[] files=["drawings.json","cases.csv","groups.csv","verdicts.json","summary.json"];
        Save(Path.Combine(final,"complete.json"),new{finalists=hash,release=Hash(release),scored=cases.Count(x=>x.Rep>=0),controls=cases.Count(x=>x.Rep<0),
            files=files.ToDictionary(x=>x,x=>Hash(Path.Combine(final,x)))});
    }
}
