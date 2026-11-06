using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentPlateStudy;

internal static partial class Program
{
    private sealed record Outcome(int Figure,string Arm,int Rep,int Parent,int Slots,string Mode,
        Scores Scores,string Ink);
    private sealed record PlateDrawing(int Figure,string Arm,int Rep,int Parent,int Slots,string Mode,
        string Path,string Policy,Scores Scores,PathTrace Trace);
    private static void Assay(string run,string releaseFile)
    {
        string output=Path.Combine(run,"final"),finalists=Path.Combine(run,"finalists.json");
        string hash=Hash(finalists);
        if(File.Exists(Path.Combine(output,"complete.json")))throw new IOException("Final already assessed.");
        var locked=Read<JsonElement>(Path.Combine(run,"lock.json"));
        if(locked.GetProperty("finalists").GetString()!=hash || locked.GetProperty("count").GetInt32()!=12 ||
            !File.ReadAllText(releaseFile).Contains(hash,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Missing release for this finalist set.");
        Directory.CreateDirectory(output);
        var winners=Read<Winner[]>(finalists);
        var rows=new List<Outcome>();var gallery=new List<PlateDrawing>();
        for(int fig=0;fig<=1;fig++)
        {
            var controls=Enumerable.Range(0,2).Select(i=>new Winner("authored-"+(i+1),-1,
                new(0,Convert.ToHexString(PlateReads.Author(i)),Convert.ToHexString(TensionMachine.DefaultPolicy()),0,0,0),0,""));
            foreach(var winner in winners.Concat(controls))
            foreach(var basis in Cases("final",0,fig))
            foreach(var mode in new[]{FeedbackMode.Live,FeedbackMode.Quiet})
            {
                var tr=TensionMachine.Run(Convert.FromHexString(winner.Selected.Path),basis.Input,basis.Slots,
                    Convert.FromHexString(winner.Selected.Policy),mode).Trace;
                var s=PlateReads.Measure(tr,fig);
                rows.Add(new(fig+1,winner.Arm,winner.Rep,basis.Parent,basis.Slots,mode.ToString(),s,PlateReads.InkKey(tr)));
                if(basis.Parent==0 && basis.Slots==9)
                    gallery.Add(new(fig+1,winner.Arm,winner.Rep,basis.Parent,basis.Slots,mode.ToString(),
                        winner.Selected.Path,winner.Selected.Policy,s,tr));
            }
        }
        using(var table=File.CreateText(Path.Combine(output,"cases.csv")))
        {
            table.WriteLine("figure,arm,rep,parent,slots,mode,historical,generic,ink,order,course,clean,exact,edges,overlap_pairs,proper_crossings,handoffs");
            foreach(var r in rows)
            {
                var s=r.Scores;
                table.WriteLine($"{r.Figure},{r.Arm},{r.Rep},{r.Parent},{r.Slots},{r.Mode},{s.Historical:R},{s.General:R},{s.Ink:R},{s.Order:R},{s.Course:R},{s.Clean:R},{s.ExactInk},{s.Edges},{s.Overlaps},{s.Crossings},{s.Handoffs}");
            }
        }
        Write(Path.Combine(output,"drawings.json"),gallery);
        var verdicts=new List<object>();
        double[] Delta(int fig,string left,string right) => Enumerable.Range(0,12).Select(p=>
        {
            var scope=rows.Where(o=>o.Parent==p && o.Figure==fig && o.Mode=="Live").ToArray();
            return scope.Where(o=>o.Arm==left).Average(o=>o.Scores.Historical)-scope.Where(o=>o.Arm==right).Average(o=>o.Scores.Historical);
        }).ToArray();
        string Direction(double[] ds) => ds.Count(d=>d>=.03)>=8 ? "supported" : ds.Count(d=>d<=-.03)>=8 ? "contrary" : "inconclusive";
        foreach(int fig in new[]{1,2})
        {
            var d=Delta(fig,"printed-start","library-generic");
            verdicts.Add(new {id="P"+fig,status=Direction(d),parentDeltas=d});
        }
        var exact=Enumerable.Range(0,4).Select(p=>{
            var rs=rows.Where(r=>r.Parent==p && r.Arm=="printed-start" && r.Mode=="Live");
            double Rate(int fig)=>rs.Where(r=>r.Figure==fig).Average(r=>r.Scores.ExactInk ? 1 : 0);
            return Rate(1)-Rate(2);
        }).ToArray();
        verdicts.Add(new{id="P3",status=exact.Count(n=>n>0)>=3 ? "supported":exact.Count(n=>n<0)>=3?"contrary":"inconclusive",parentDeltas=exact});
        var refCalls=rows.Where(r=>r.Parent<4 && r.Arm=="authored-"+r.Figure && r.Mode=="Quiet").ToArray();
        verdicts.Add(new{id="P4",status=refCalls.All(r=>r.Scores.ExactInk)?"supported":"contrary",
            exact=refCalls.Count(r=>r.Scores.ExactInk),total=refCalls.Length});
        var live=rows.Where(r=>r.Figure==2 && !r.Arm.StartsWith("authored") && r.Mode=="Live").ToArray();
        var quiet=rows.Where(r=>r.Figure==2 && !r.Arm.StartsWith("authored") && r.Mode=="Quiet").ToDictionary(r=>(r.Arm,r.Rep,r.Parent,r.Slots));
        var gains=live.GroupBy(r=>r.Parent).OrderBy(g=>g.Key)
            .Select(g=>g.Average(r=>r.Scores.Historical-quiet[(r.Arm,r.Rep,r.Parent,r.Slots)].Scores.Historical)).ToArray();
        int material=live.GroupBy(r=>r.Parent).Count(g=>g.Any(r=>r.Ink!=quiet[(r.Arm,r.Rep,r.Parent,r.Slots)].Ink));
        verdicts.Add(new{id="P5",status=material>=6 && Direction(gains)=="inconclusive"?"supported":"inconclusive",
            parentsWithChange=material,parentDeltas=gains});
        Write(Path.Combine(output,"verdicts.json"),verdicts);
        var summary=rows.GroupBy(r=>(r.Figure,r.Arm,r.Mode)).Select(g=>new{g.Key.Figure,g.Key.Arm,g.Key.Mode,
            historical=g.Average(r=>r.Scores.Historical),ink=g.Average(r=>r.Scores.Ink),
            order=g.Average(r=>r.Scores.Order),course=g.Average(r=>r.Scores.Course),
            exact=g.Count(r=>r.Scores.ExactInk),total=g.Count()}).ToArray();
        Write(Path.Combine(output,"summary.json"),summary);
        var checks=new List<object>();
        for(int figure=0;figure<2;figure++)
        {
            var input=new PathInput(4,2,PlateReads.Period[figure],1+3);
            var reference=PathMachine.Run(PlateReads.Author(figure),input);
            var reversed=reference with {Edges=reference.Edges.Reverse().Select((e,i)=>e with
                {From=e.To,To=e.From,Direction=(e.Direction+4)&7,Beat=i}).ToArray()};
            var doubled=reference with {Edges=[..reference.Edges,reference.Edges[0]]};
            var native=PlateReads.Measure(reference,figure);
            var backward=PlateReads.Measure(reversed,figure);
            var retraced=PlateReads.Measure(doubled,figure);
            if(!native.ExactInk || native.Order!=1 || !backward.ExactInk || backward.Order>=native.Order ||
                !retraced.ExactInk || retraced.Overlaps==0 || retraced.Clean>=native.Clean)
                throw new InvalidDataException("Historical reader conflated order, support or repeated deposits.");
            checks.Add(new{figure=figure+1,native,backward,retraced});
        }
        Write(Path.Combine(output,"challenges.json"),checks);
        string[] files=["cases.csv","verdicts.json","drawings.json","summary.json","challenges.json"];
        Write(Path.Combine(output,"complete.json"),new {finalists=hash,jobs=rows.Count,release=Hash(releaseFile),
            files=files.ToDictionary(f=>f,f=>Hash(Path.Combine(output,f)))});
    }
}
