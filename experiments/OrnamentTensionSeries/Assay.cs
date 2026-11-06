using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentTensionSeries;

internal static partial class Program
{
    private sealed record Decision(string Name,double Margin,string Direction,Func<Collection,double> Read,SeriesMode Compared);
    private static readonly Decision[] Questions=[
        new("wider-than-hold",.015,"higher",r=>r.Novelty,SeriesMode.Hold),
        new("wider-than-clock",.01,"higher",r=>r.Novelty,SeriesMode.Clock),
        new("pulses-add-variety",.005,"higher",r=>r.Novelty,SeriesMode.TrayOnly),
        new("choices-add-variety",.01,"higher",r=>r.Novelty,SeriesMode.PulseOnly),
        new("fewer-exact-copies",.10,"higher",r=>r.Exact,SeriesMode.Hold),
        new("reference-preserved",.03,"equivalent",r=>r.Reference,SeriesMode.Clock),
        new("repeat-preserved",.03,"equivalent",r=>r.Repeat,SeriesMode.Clock)];

    private static void Assay(string dir,string release)
    {
        string file=Path.Combine(dir,"finalists.json"), digest=Hash(file);
        if(!File.ReadAllText(release).Contains(digest,StringComparison.OrdinalIgnoreCase) ||
            Load<JsonElement>(Path.Combine(dir,"lock.json")).GetProperty("finalists").GetString()!=digest)
            throw new InvalidDataException("Common reserve release must bind the series finalists.");
        var selected=Load<SeriesLock>(file);
        if(selected.Seal!=Hash(Path.Combine(dir,"seal.json")) || selected.Winners.Length!=EngineLibrary.Names.Length*4)
            throw new InvalidDataException("Invalid series finalists.");
        var outDir=Path.Combine(dir,"reserve");
        if(Directory.Exists(outDir) && Directory.EnumerateFileSystemEntries(outDir).Any())throw new IOException("Assay already started.");
        Directory.CreateDirectory(outDir);
        var cohort=Cases(dir,"reserve");
        var studies=new List<Scored>();
        var examples=new List<(Sheet,Member[])>();
        long sameAsTray=0,changedAsTray=0;
        using(var cases=File.CreateText(Path.Combine(outDir,"collections.csv")))
        using(var members=new StreamWriter(new GZipStream(File.Create(Path.Combine(outDir,"members.csv.gz")),CompressionLevel.Fastest),new UTF8Encoding(false)))
        {
            cases.WriteLine("family,rep,parent,setting,slots,mode,novelty,exact,broad,reference,repeat,grade,switches,pulses,echoes,ink_outcomes,echo_near,echo_far,missed_near");
            members.WriteLine("family,rep,setting,mode,member,tray,echo,before,spent,after,overflow,lane,pulse,publication,ink,broad,reference,repeat,lower_acts");
            foreach(var champ in selected.Winners)
            foreach(var setting in cohort)
            {
                var tray=champ.Tray;
                var bank=new SeriesDraw.Bank(tray,setting,true);
                string[]? actual=null;
                foreach(var mode in Enum.GetValues<SeriesMode>())
                {
                    var sequence=SeriesDraw.Members(bank,Convert.FromHexString(champ.Rule),mode,tray.Replicate&1);
                    var m=SeriesDraw.Measure(sequence);
                    studies.Add(new(champ,setting,mode,m));
                    cases.WriteLine(string.Join(',',EngineLibrary.Names[tray.Family],tray.Replicate,setting.Parent,setting.Key,setting.Slots,mode,
                        F(m.Novelty),F(m.Exact),F(m.Broad),F(m.Reference),F(m.Repeat),F(m.Grade),m.Switches,m.Pulses,m.DeclaredEchoes,
                        m.ActualShapes,m.EchoNear,m.EchoFar,m.MissedNear));
                    if(mode==SeriesMode.Live)actual=sequence.Select(x=>x.Ink).ToArray();
                    if(mode==SeriesMode.TrayOnly)
                    {
                        if(actual!.SequenceEqual(sequence.Select(x=>x.Ink)))sameAsTray++;
                        else changedAsTray++;
                    }
                    foreach(var (step,i) in sequence.Select((m,i)=>(m,i)))
                    {
                        var p=step.Move;
                        members.WriteLine(string.Join(',',EngineLibrary.Names[tray.Family],tray.Replicate,setting.Key,mode,i,
                            p.Tool,p.Echo,p.Before,p.Spent,p.After,p.Overflow,p.Lane,p.Pulse,step.Publication,step.Ink,
                            F(step.Broad),F(step.Reference),F(step.Repeat),step.Actuations));
                    }
                    if(setting.Parent=="reserve-00" && setting.Slots==5 && setting.Input.SeedBit==0 && tray.Replicate==tray.Family%4)
                        examples.Add((new(tray,champ.Rule,setting,mode),sequence));
                }
            }
        }
        var live=studies.Where(x=>x.Mode==SeriesMode.Live).ToArray();
        var byKey=studies.ToDictionary(r=>(r.Pick.Tray.Family,r.Pick.Tray.Replicate,r.Setting.Key,r.Mode));
        using(var f=File.CreateText(Path.Combine(outDir,"groups.csv")))
        using(var verdicts=File.CreateText(Path.Combine(outDir,"verdicts.csv")))
        {
            f.WriteLine("question,parent,delta");
            verdicts.WriteLine("question,direction,margin,parents,positive,negative,within,mean,status");
            foreach(var q in Questions)
            {
                var means=live.GroupBy(r=>r.Setting.Parent).Select(g=>(Parent:g.Key,Value:g.Average(r=>
                    q.Read(r.Measures)-q.Read(byKey[(r.Pick.Tray.Family,r.Pick.Tray.Replicate,r.Setting.Key,q.Compared)].Measures)))).ToArray();
                foreach(var (parent,value) in means)f.WriteLine($"{q.Name},{parent},{F(value)}");
                int up=means.Count(x=>x.Value>=q.Margin),down=means.Count(x=>x.Value<=-q.Margin),within=means.Length-up-down;
                string status=q.Direction=="higher" ? (up>=8?"supported":down>=8?"contrary":"inconclusive") :
                    (within>=8?"supported":Math.Max(up,down)>=8?"contrary":"inconclusive");
                verdicts.WriteLine($"{q.Name},{q.Direction},{F(q.Margin)},{means.Length},{up},{down},{within},{F(means.Average(x=>x.Value))},{status}");
            }
        }
        var summary=studies.GroupBy(x=>x.Mode).Select(g=>new {mode=g.Key.ToString(),collections=g.Count(),
            novelty=g.Average(x=>x.Measures.Novelty), exact=g.Average(x=>x.Measures.Exact),broad=g.Average(x=>x.Measures.Broad),
            reference=g.Average(x=>x.Measures.Reference),repeat=g.Average(x=>x.Measures.Repeat),grade=g.Average(x=>x.Measures.Grade),
            programs=g.Average(x=>x.Measures.Switches==0?1:2),material=g.Average(x=>x.Measures.ActualShapes),
            echoNear=g.Sum(x=>x.Measures.EchoNear),echoFar=g.Sum(x=>x.Measures.EchoFar),missedNear=g.Sum(x=>x.Measures.MissedNear),
            signals=g.Sum(x=>x.Measures.Pulses) });
        Save(Path.Combine(outDir,"summary.json"),new {modes=summary,sameAsTray,changedAsTray});
        // Registered examples use the first reserve parent/ordinary realization
        // for one fixed replicate per engine. Render only actual executions.
        var rendered=examples.Select(e=>new {sheet=e.Item1,steps=e.Item2.Select((m,i)=>new{
            m.Move,m.Publication,m.Ink,m.Broad,m.Reference,trace=SeriesDraw.Execute(e.Item1.Tray,e.Item1.Setting,i,m.Move).Trace})});
        Save(Path.Combine(outDir,"examples.json"),rendered);
        var cold=examples.First(e=>e.Item1.Mode==SeriesMode.Live).Item1;
        Save(Path.Combine(outDir,"replay.json"),cold);
        Play(cold,Path.Combine(outDir,"expected.json"));
        string[] records=["collections.csv","members.csv.gz","verdicts.csv","groups.csv","examples.json","summary.json","replay.json","expected.json"];
        Save(Path.Combine(outDir,"complete.json"),new {finalists=digest,release=Hash(release),collections=studies.Count,
            episodes=studies.Count*SeriesDraw.Length,files=records.ToDictionary(x=>x,x=>Hash(Path.Combine(outDir,x)))});
    }
}
