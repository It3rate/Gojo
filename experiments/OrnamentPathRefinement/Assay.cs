using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentPathRefinement;

internal static partial class Program
{
    private sealed record Case(string Brief,string Arm,int Rep,Scene Scene,string Hex,Reading Read,int Edges);
    private sealed record Prediction(string Brief,string Contrast,string Left,string Right,string Read,string Expected,
        double Margin,int Parents,int Positive,int Negative,int Within,double Mean,string Status);

    private static void Assay(string run,string release)
    {
        var locked=JsonDocument.Parse(File.ReadAllText(Path.Combine(run,"lock.json"))).RootElement;
        string hash=Hash(Path.Combine(run,"finalists.json"));
        if(locked.GetProperty("finalists").GetString()!=hash||locked.GetProperty("count").GetInt32()!=24||
            locked.GetProperty("seal").GetString()!=Hash(Path.Combine(run,"seal.json"))||
            !File.ReadAllText(release).Contains(hash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Shared release must bind these 24 frozen finalists.");
        string dir=Path.Combine(run,"reserve");
        if(File.Exists(Path.Combine(dir,"complete.json")))throw new IOException("Reserve already recorded.");
        Directory.CreateDirectory(dir);
        var finalists=Read<Winner[]>(Path.Combine(run,"finalists.json"));
        using var design=JsonDocument.Parse(File.ReadAllText(Path.Combine(run,"wave-design.json")));
        var scenes=design.RootElement.GetProperty("reserve").Deserialize<Scene[]>(Json)!;
        var cases=new List<Case>();
        foreach(var winner in finalists)
        {
            var weights=design.RootElement.GetProperty("weights").GetProperty(PrimaryBrief(winner.Brief)).Deserialize<double[]>(Json)!;
            foreach(var s in scenes)
            {
                var trace=PathMachine.Run(Convert.FromHexString(winner.Hex),s.Input);
                cases.Add(new(winner.Brief,winner.Arm,winner.Rep,s,winner.Hex,Evaluate(winner.Brief,trace,weights),trace.Edges.Length));
            }
        }
        using(var f=File.CreateText(Path.Combine(dir,"cases.csv")))
        {
            f.WriteLine("brief,arm,rep,parent,scene,hex,original,history,witness,edges");
            foreach(var q in cases)f.WriteLine($"{q.Brief},{q.Arm},{q.Rep},{q.Scene.Parent},{q.Scene.Name},{q.Hex},{F(q.Read.Original)},{F(q.Read.History)},{F(q.Read.Witness)},{q.Edges}");
        }
        var verdicts=new List<Prediction>();
        var groups=new List<string>{"brief,contrast,parent,delta"};
        foreach(string brief in Briefs)
        {
            var lookup=cases.Where(x=>x.Brief==brief).ToDictionary(x=>(x.Arm,x.Rep,x.Scene.Name));
            foreach(var (left,right,read,expect,margin) in new[]{("blend","baseline","history","higher",.04),("blend","baseline","witness","higher",.10),
                ("history","baseline","history","higher",.04),("history","baseline","witness","higher",.10),("blend","history","original","higher",.04),
                ("blend","baseline","original","equivalent",.04)})
            {
                double Value(Reading x)=>read switch{"history"=>x.History,"witness"=>x.Witness,_=>x.Original};
                string key=$"{left}-{right}-{read}";
                var ds=new List<double>();
                foreach(var parent in scenes.GroupBy(s=>s.Parent))
                {
                    double delta=parent.SelectMany(s=>Enumerable.Range(0,Replicates).Select(r=>
                        Value(lookup[(left,r,s.Name)].Read)-Value(lookup[(right,r,s.Name)].Read))).Average();
                    ds.Add(delta);groups.Add($"{brief},{key},{parent.Key},{F(delta)}");
                }
                int pos=ds.Count(n=>n>=margin),neg=ds.Count(n=>n<=-margin),within=ds.Count(n=>Math.Abs(n)<margin);
                string status=expect=="equivalent" ? within>=8 ? "supported" : Math.Max(pos,neg)>=8 ? "contrary":"inconclusive"
                    : pos>=8?"supported":neg>=8?"contrary":"inconclusive";
                verdicts.Add(new(brief,key,left,right,read,expect,margin,ds.Count,pos,neg,within,ds.Average(),status));
            }
        }
        Save(Path.Combine(dir,"verdicts.json"),verdicts);
        File.WriteAllLines(Path.Combine(dir,"groups.csv"),groups);
        var exhibits=new List<object>(); var summary=new List<string>{"brief,arm,cases,original,history,witness,without_strong_witness,mean_edges"};
        foreach(var brief in Briefs)
        {
            int rep=finalists.Where(w=>w.Brief==brief).GroupBy(w=>w.Rep).OrderByDescending(g=>g.Average(w=>Score(w.Validation,w.Arm))).ThenBy(g=>g.Key).First().Key;
            foreach(var w in finalists.Where(w=>w.Brief==brief))
            {
                var c=cases.Single(q=>q.Brief==brief&&q.Arm==w.Arm&&q.Rep==w.Rep&&q.Scene.Name==scenes[0].Name);
                var t=PathMachine.Run(Convert.FromHexString(w.Hex),c.Scene.Input);
                exhibits.Add(new{brief,arm=w.Arm,replicate=w.Rep,representative=w.Rep==rep,scenario=c.Scene,w.Hex,
                    c.Read,trace=t});
            }
            foreach(var g in cases.Where(c=>c.Brief==brief).GroupBy(c=>c.Arm))
                summary.Add($"{brief},{g.Key},{g.Count()},{F(g.Average(x=>x.Read.Original))},{F(g.Average(x=>x.Read.History))},{F(g.Average(x=>x.Read.Witness))},{g.Count(x=>x.Read.Witness==0)},{F(g.Average(x=>x.Edges))}");
        }
        File.WriteAllLines(Path.Combine(dir,"summary.csv"),summary);
        Save(Path.Combine(dir,"exhibits.json"),exhibits);
        string[] files=["cases.csv","groups.csv","verdicts.json","summary.csv","exhibits.json"];
        Save(Path.Combine(dir,"complete.json"),new{hash,release=Hash(release),cases=cases.Count,
            files=files.ToDictionary(k=>k,k=>Hash(Path.Combine(dir,k)))});
    }
}
