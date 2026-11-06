using System.Text.Json;

namespace Gojo.Experiments.OrnamentPathCampaign;

internal sealed record Observed(string Brief,string Arm,int Replicate,Scenario Scenario,string Hex,
    double Score,string Digest,double[] Metrics,int Edges,int Branches,int Merges,int Switches,bool Viable);

internal static class Assay
{
    public static string Prediction(Brief b,string comparator)
    {
        if(comparator=="frozen") return b.Name is "countermarch" or "branched-spray" ? "higher" : "equivalent";
        if(comparator is "cut" or "open") return b.Name is "branched-spray" or "stepped-lace" ? "higher" : "equivalent";
        if(comparator.StartsWith("omit-"))
            return PathJudge.Criteria.Any(c=>c.Group==comparator[5..] && b.Emphasis.Contains(c.Key))
                ? "higher" : "equivalent";
        return "higher";
    }

    public static Observed Observe(Winner w,Scenario scenario,string arm,PathMode mode, double[] weights)
    {
        var trace=PathMachine.Run(Convert.FromHexString(w.Hex),scenario.Input,mode);
        var metrics=PathJudge.Measure(trace);
        bool viable=Competition.Viable(trace);
        double score=viable ? metrics.Select((n,i)=>double.IsNaN(n) ? 0 : n*weights[i]).Sum() : 0;
        int Count(string kind)=>trace.Events.Count(e=>e.Kind==kind);
        return new(w.Brief,arm,w.Replicate,scenario,w.Hex,score,Program.TraceDigest(trace),metrics,trace.Edges.Length,
            Count("branch"),Count("merge"),Count("switch"),viable);
    }
    public static void Run(string folder)
    {
        var finalists=Program.Locked(folder);
        if(File.Exists(Path.Combine(folder,"complete.json"))) throw new IOException("Blind study already complete.");
        var cases=Design.Scenarios("blind");
        var observations=new List<Observed>();
        foreach(var b in Design.Briefs)
        {
            var weights=Design.Weights(b,"full");
            var foundation=finalists.Foundations.Single(w=>w.Brief==b.Name);
            foreach(var w in finalists.Winners.Where(w=>w.Brief==b.Name))
                foreach(var s in cases)
                {
                    observations.Add(Observe(w,s,w.Arm,PathMode.Live,weights));
                    if(w.Arm!="full") continue;
                    observations.Add(Observe(foundation with {Replicate=w.Replicate},s,"foundation",PathMode.Live,weights));
                    foreach(var mode in new[]{PathMode.Cut,PathMode.Frozen,PathMode.Open})
                        observations.Add(Observe(w,s,mode.ToString().ToLowerInvariant(),mode,weights));
                }
        }
        using(var f=Record.GzipWriter(Path.Combine(folder,"blind-cases.csv.gz")))
        {
            f.WriteLine("brief,arm,replicate,parent,scenario,genome,utility,edges,branches,merges,switches,viable,trace,"+
                string.Join(",",PathJudge.Criteria.Select(c=>c.Key)));
            foreach(var o in observations)
                f.WriteLine(Record.Csv(o.Brief,o.Arm,o.Replicate,o.Scenario.Parent,o.Scenario.Name,o.Hex,o.Score,o.Edges,
                    o.Branches,o.Merges,o.Switches,o.Viable,o.Digest)+","+string.Join(",",o.Metrics.Select(n=>double.IsNaN(n) ? "" : Record.F(n))));
        }
        Compare(folder,observations);
        Exhibits(folder,observations,finalists);
        var jobs=observations.Where(o=>o.Scenario.Name=="blind-00-0")
            .Select(o=>new ReplayJob($"{o.Brief}/{o.Replicate}/{o.Arm}",o.Hex,o.Scenario.Input,Mode(o.Arm))).ToArray();
        Record.Write(Path.Combine(folder,"replay-inputs.json"),jobs);
        Record.Write(Path.Combine(folder,"replay-expected.json"),observations.Where(o=>o.Scenario.Name=="blind-00-0")
            .Select(o=> new {id=$"{o.Brief}/{o.Replicate}/{o.Arm}",digest=o.Digest}).ToArray());
        Record.Write(Path.Combine(folder,"challenges.json"),JudgeChallenge.Run());
        string[] results=["blind-cases.csv.gz","verdicts.csv","group-comparison.csv","exhibits.json","replay-inputs.json","replay-expected.json","challenges.json"];
        Record.Write(Path.Combine(folder,"complete.json"),new { finalists=Record.Hash(Path.Combine(folder,"finalists.json")),
            cases=observations.Count,completed=DateTime.UtcNow.ToString("O"),
            files=results.ToDictionary(p=>p,p=>Record.Hash(Path.Combine(folder,p))) });
    }
    public static PathMode Mode(string arm) => arm switch {
        "open"=>PathMode.Open,"cut"=>PathMode.Cut,"frozen"=>PathMode.Frozen,_=>PathMode.Live };

    private static void Compare(string folder,List<Observed> all)
    {
        using var group=File.CreateText(Path.Combine(folder,"group-comparison.csv"));
        using var results=File.CreateText(Path.Combine(folder,"verdicts.csv"));
        group.WriteLine("brief,arm,replicate,full_mean,arm_mean,difference,positive,negative,within");
        results.WriteLine("brief,contrast,stratum,predicted,margin,parents,positive,negative,within,mean_delta,status");
        var native=all.Where(o=>o.Arm=="full").ToDictionary(o=>(o.Brief,o.Replicate,o.Scenario.Name));
        foreach(var comparison in all.Where(o=>o.Arm!="full").GroupBy(o=>(o.Brief,o.Arm)))
        {
            var (brief,arm)=comparison.Key;
            double Delta(Observed o)=>native[(brief,o.Replicate,o.Scenario.Name)].Score-o.Score;
            foreach(var sample in comparison.GroupBy(o=>o.Replicate))
            {
                var ds=sample.GroupBy(o=>o.Scenario.Parent).Select(p=>p.Average(Delta)).ToArray();
                group.WriteLine(Record.Csv(brief,arm,sample.Key,sample.Average(o=>native[(brief,o.Replicate,o.Scenario.Name)].Score),
                    sample.Average(o=>o.Score),ds.Average(),ds.Count(n=>n>=.02),ds.Count(n=>n<=-.02),ds.Count(n=>Math.Abs(n)<.02)));
            }
            var parentMeans=comparison.GroupBy(o=>o.Scenario.Parent).OrderBy(g=>g.Key,StringComparer.Ordinal)
                .Select(p=>p.Average(Delta)).ToArray();
            string expect=Prediction(Design.Briefs.Single(b=>b.Name==brief),arm);
            foreach(var (name,delta) in new[]{("all",parentMeans),("basis-A",parentMeans[0..3]),
                      ("basis-B",parentMeans[3..6]),("basis-C",parentMeans[6..9]),("basis-D",parentMeans[9..12])})
            {
                int up=delta.Count(n=>n>=.02),down=delta.Count(n=>n<=-.02),within=delta.Length-up-down;
                int required=delta.Length==12 ? 8 : 3;
                string status=expect=="equivalent" ?
                    (within>=required ? "supported" : Math.Max(up,down)>=required ? "contrary" : "inconclusive") :
                    (up>=required ? "supported" : down>=required ? "contrary" : "inconclusive");
                results.WriteLine(Record.Csv(brief,arm,name,expect,.02,delta.Length,up,down,within,delta.Average(),status));
            }
        }
    }

    public static object Exhibit(Observed o,string id,string label,string description)
    {
        var trace=PathMachine.Run(Convert.FromHexString(o.Hex),o.Scenario.Input,Mode(o.Arm));
        if(Program.TraceDigest(trace)!=o.Digest) throw new InvalidDataException(id);
        double? Known(double n)=>double.IsNaN(n) ? null : n;
        var measures=PathJudge.Criteria.Select((c,i)=>new {c.Key,c.Group,Value=Known(o.Metrics[i])}).ToArray();
        return new {id,brief=o.Brief,arm=o.Arm,label,description,scenario=o.Scenario.Name,genome=o.Hex,
            realization=o.Scenario.Input,utility=o.Score,periodX=trace.Pitch,seamX=0,cells=trace.Cells,
            sites=trace.Sites,events=trace.Events, owners=trace.Owners, finalGenome=Convert.ToHexString(trace.FinalProgram),
            metrics=measures.ToDictionary(c=>c.Key,c=>c.Value),groups=measures.GroupBy(c=>c.Group).ToDictionary(g=>g.Key,g=>g.Average(c=>c.Value)),
            strokes=trace.Edges.Select(e=>new {a=e.From,b=e.To,x1=trace.Sites[e.From].X,y1=trace.Sites[e.From].Y,
                x2=trace.Sites[e.To].X,y2=trace.Sites[e.To].Y,e.Owner,e.Beat,e.Cell,e.Row,e.Direction,e.Extent})};
    }
    private static void Exhibits(string folder,List<Observed> all,Finalists finalists)
    {
        var list=new List<object>();
        foreach(var b in Design.Briefs)
        {
            var entrants=finalists.Winners.Where(w=>w.Brief==b.Name && w.Arm=="full");
            var representative=entrants.OrderByDescending(w=>w.ValidationUtility).ThenBy(w=>w.Replicate).First();
            var own=all.Where(o=>o.Brief==b.Name && o.Replicate==representative.Replicate).ToArray();
            var live=own.Where(o=>o.Arm=="full").ToArray();
            void Add(Observed o,string suffix,string label,string description)=>list.Add(Exhibit(o,$"{b.Name}-{suffix}",label,description));
            Add(live[0],"first","Validation-selected machine",b.Aim+" First preselected test input.");
            Add(live.MaxBy(o=>o.Score)!,"best","Highest blind read","Best tested realization of the same finalist.");
            Add(live.MinBy(o=>o.Score)!,"worst","Lowest blind read","Weakest tested realization of the same finalist.");
            foreach(string arm in new[]{"field","random","unseeded","foundation","cut","frozen"})
                Add(own.First(o=>o.Arm==arm),arm,arm+" comparison","Same physical input as the first drawing.");
            foreach(int rep in Enumerable.Range(0,4).Where(r=>r!=representative.Replicate))
                Add(all.First(o=>o.Brief==b.Name && o.Replicate==rep && o.Arm=="full"),"rep"+rep,
                    "Independent search "+(rep+1), "First preselected test input.");
        }
        Record.Write(Path.Combine(folder,"exhibits.json"),new { examples=list });
    }
}
