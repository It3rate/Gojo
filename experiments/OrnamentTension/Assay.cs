using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentTension;

internal sealed record Observation(int Family,int Replicate,string Arm,Setting Setting,Recipe Recipe,double Broad,double Reference,
    double Wrong,double[] Values,string Ink,double Travel,int Range,int Tips,int Crossings,int Overlap,int Branches,int Merges,
    int Edges,int Owners,FeedbackStats Pressure,IReadOnlyDictionary<string,int> Turns,IReadOnlyDictionary<string,int> Forks,
    byte Publication,double[] Details);
internal static class Assay
{
    public static readonly string[] Details=["connected-course","whole-course-span","shared-seam","no-overdraw","slot-reach",
        "turn-variety","motif-mirror","alternating-mirror","period-match","cell-variation","shape-novelty","winding-terminal"];
    public static string Prediction(string contrast)=>contrast=="static" ? "equivalent" : "higher";
    public static double Margin(string contrast)=>contrast=="wrong-reference" ? .04 : .012;
    public static string Ink(PathTrace trace)
    {
        // Unoriented union on the native axial/diagonal basis. Splitting an edge
        // at another site changes the history, but leaves this deposited ink equal.
        var atoms=new HashSet<(int X,int Y,int Dx,int Dy)>();
        foreach(var e in trace.Edges)
        {
            var a=trace.Sites[e.From];var b=trace.Sites[e.To];
            if(a.X>b.X||a.X==b.X&&a.Y>b.Y)(a,b)=(b,a);
            int dx=Math.Sign(b.X-a.X),dy=Math.Sign(b.Y-a.Y);
            int length=Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));
            for(int k=0;k<length;k++)atoms.Add((a.X+k*dx,a.Y+k*dy,dx,dy));
        }
        return Files.DigestObject(atoms.Order().Select(a=>new[]{a.X,a.Y,a.Dx,a.Dy}));
    }
    public static Observation Observe(Winner w,string arm,Setting s)
    {
        var (result,read)=Search.Draw(w.Recipe,s,w.Family,Design.Mode(arm),true);
        var t=result.Trace;
        var joints=JointCensus.Read(t);
        int vertical=t.Edges.Sum(e=>Math.Abs(t.Sites[e.To].Y-t.Sites[e.From].Y));
        var ys=t.Edges.SelectMany(e=>new[]{t.Sites[e.From].Y,t.Sites[e.To].Y}).ToArray();
        return new(w.Family,w.Replicate,arm,s,w.Recipe,read.Broad,read.Reference,read.WrongReference,read.Values,Ink(t),
            (double)vertical/(s.Input.Pitch*s.Input.Cells),ys.Length>0 ? ys.Max()-ys.Min() : 0,joints.LooseTips,
            joints.InteriorCrossings,joints.CollinearOverlaps,joints.BranchSites,joints.MergeSites,t.Edges.Length,t.Owners,result.Stats,
            joints.TurnPairs,joints.ForkPairs,result.Publication,Details.Select(k=>read.Raw[k]).ToArray());
    }
    public static void Run(string folder,int workers)
    {
        if(File.Exists(Path.Combine(folder,"blind.lock")))throw new IOException("Blind run completed.");
        var finalists=Program.Frozen(folder);
        var bank=Design.Settings("blind");
        var work=new List<(Winner W,string Arm)>();
        foreach(var w in finalists.Winners)
        {
            work.Add((w,w.Arm));
            if(w.Arm!="coupled")continue;
            foreach(string mode in new[]{"disconnect","static","cut-up","cut-down","wrong-route"})work.Add((w,mode));
            work.Add((w with {Recipe=new(Convert.ToHexString(EngineLibrary.Create(w.Family)),Convert.ToHexString(TensionMachine.DefaultPolicy()))},"source"));
        }
        var parts=new Observation[work.Count][];
        Parallel.For(0,work.Count,new ParallelOptions{MaxDegreeOfParallelism=workers},i=>{
            var (w,arm)=work[i];parts[i]=bank.Select(s=>Observe(w,arm,s)).ToArray();
        });
        var rows=parts.SelectMany(x=>x).ToArray();
        using(var f=Files.Gzip(Path.Combine(folder,"blind.csv.gz")))
        {
            f.WriteLine("family,rep,arm,parent,scenario,slots,broad,matched_reference,wrong_reference,ink,travel,range,tips,crossings,overlaps,forks,merges,edges,offered,accepted,blocked,in_flight,actions,slot_blocks,relief,misses,route_changes,pen_debt,machine_debt,frieze_debt,publication,"+string.Join(",",TensionCritics.Names.Concat(Details)));
            foreach(var o in rows)
            {
                var p=o.Pressure;
                f.WriteLine(string.Join(",",new object[]{EngineLibrary.Names[o.Family],o.Replicate,o.Arm,o.Setting.Parent,o.Setting.Key,
                    o.Setting.Slots,o.Broad,o.Reference,o.Wrong,o.Ink,o.Travel,o.Range,o.Tips,o.Crossings,o.Overlap,o.Branches,o.Merges,o.Edges,
                    p.Offered,p.Accepted,p.Blocked,p.Remainder,p.Actuations,p.SlotBlocks,p.Relief,p.Misses,p.LearnedChanges,
                    p.PenDebt,p.MachineDebt,p.FriezeDebt,o.Publication})+","+string.Join(",",o.Values.Concat(o.Details).Select(Files.F)));
            }
        }
        var live=rows.Where(o=>o.Arm=="coupled").ToDictionary(o=>(o.Family,o.Replicate,o.Setting.Key));
        using(var f=Files.Gzip(Path.Combine(folder,"joint-uses.csv.gz")))
        {
            f.WriteLine("family,rep,arm,scenario,slots,kind,sequence,occurrences,broad");
            foreach(var o in rows)foreach(var (kind,entries) in new[]{("turn",o.Turns),("fork",o.Forks)})
                foreach(var (key,count) in entries)
                    f.WriteLine($"{EngineLibrary.Names[o.Family]},{o.Replicate},{o.Arm},{o.Setting.Key},{o.Setting.Slots},{kind},\"{key}\",{count},{Files.F(o.Broad)}");
        }
        using(var f=File.CreateText(Path.Combine(folder,"route-activity.csv")))
        {
            f.WriteLine("family,slots,arm,from,to,lane,routes,offers,accepted,blocked,relief,misses,mean_final_strength");
            string Tier(int site,int n)=>site<n?"pen":site==n?"machine":"frieze";
            var routes=from o in rows from r in o.Pressure.Routes group r by (o.Family,o.Setting.Slots,o.Arm,
                From:Tier(r.Source,o.Owners),To:Tier(r.Target,o.Owners),r.Lane);
            foreach(var g in routes)
            {
                var k=g.Key;
                f.WriteLine($"{EngineLibrary.Names[k.Family]},{k.Slots},{k.Arm},{k.From},{k.To},{k.Lane},{g.Count()},{g.Sum(r=>r.Offered)},{g.Sum(r=>r.Accepted)},{g.Sum(r=>r.Blocked)},{g.Sum(r=>r.Relief)},{g.Sum(r=>r.Misses)},{Files.F(g.Average(r=>r.Strength))}");
            }
        }
        using(var f=File.CreateText(Path.Combine(folder,"contrasts.csv")))
        {
            f.WriteLine("family,slots,contrast,predicted,margin,parents,up,down,within,mean_delta,changed_material,cases,status");
            foreach(int family in Enumerable.Range(0,EngineLibrary.Names.Length))foreach(int slots in Design.Slots)
            foreach(string contrast in Design.Contrasts)
            {
                bool reference=contrast=="wrong-reference";
                var matches=rows.Where(o=>o.Family==family&&o.Setting.Slots==slots&&o.Arm==(reference ? "coupled" : contrast)).ToArray();
                double Delta(Observation o)=>reference ? o.Reference-o.Wrong : live[(family,o.Replicate,o.Setting.Key)].Broad-o.Broad;
                double margin=Margin(contrast);var means=matches.GroupBy(o=>o.Setting.Parent).Select(g=>g.Average(Delta)).ToArray();
                int up=means.Count(x=>x>=margin),down=means.Count(x=>x<=-margin),within=means.Length-up-down;
                string expected=Prediction(contrast);
                string status=expected=="equivalent" ? (within>=8?"supported":Math.Max(up,down)>=8?"contrary":"inconclusive") :
                    up>=8?"supported":down>=8?"contrary":"inconclusive";
                int changed=matches.Count(o=>live[(family,o.Replicate,o.Setting.Key)].Ink!=o.Ink);
                f.WriteLine($"{EngineLibrary.Names[family]},{slots},{contrast},{expected},{margin},{means.Length},{up},{down},{within},{Files.F(means.Average())},{changed},{matches.Length},{status}");
            }
        }
        var samples=new List<object>();var jobs=new List<Job>();var expectedReplay=new List<object>();
        foreach(int family in Enumerable.Range(0,EngineLibrary.Names.Length))
        {
            // Reserve no editorial discretion in these examples. One replicate
            // per family, first blind basis and ordinary phase, at both extremes.
            int rep=family%Design.Replicates;
            var subset=rows.Where(o=>o.Family==family&&o.Replicate==rep&&o.Setting.Parent=="blind-00"&&
                o.Setting.Input.SeedBit==0&&o.Setting.Slots!=5).ToArray();
            foreach(var o in subset.Where(o=>o.Arm is "coupled" or "disconnect" or "source"))
            {
                var s=o.Setting;var (run,read)=Search.Draw(o.Recipe,s,family,Design.Mode(o.Arm),true,true);
                if(o.Ink!=Ink(run.Trace)||o.Broad!=read.Broad)throw new InvalidDataException("Exhibit replay mismatch");
                string id=$"{EngineLibrary.Names[family]}/{o.Arm}/{s.Slots}";
                samples.Add(new {id,engine=EngineLibrary.Names[family],o.Arm,o.Replicate,o.Recipe,setting=s,broad=o.Broad,
                    components=read.Values,raw=read.Raw.ToDictionary(x=>x.Key,x=>double.IsFinite(x.Value)?x.Value:(double?)null),
                    joints=JointCensus.Read(run.Trace,true),trace=run.Trace,pressure=run.Stats,timeline=run.Timeline,run.Publication});
                jobs.Add(new(id,o.Recipe,s.Input,s.Slots,Design.Mode(o.Arm)));
                expectedReplay.Add(new{id,digest=Files.DigestObject(run)});
            }
        }
        Files.Save(Path.Combine(folder,"exhibits.json"),samples);
        Files.Save(Path.Combine(folder,"replay-inputs.json"),jobs);Files.Save(Path.Combine(folder,"replay-expected.json"),expectedReplay);
        Files.Save(Path.Combine(folder,"joints.json"),JointCensus.Challenges());
        var foundations=new List<object>();
        for(int family=0;family<EngineLibrary.Names.Length;family++)
        {
            const int a=4,b=2;
            int pitch=family switch {0 or 7=>a+2*b,1=>a+2*b,6=>2*a+3*b,_=>2*a};
            var program=EngineLibrary.Create(family);var input=new PathInput(a,b,pitch,4,0,0);
            var trace=PathMachine.Run(program,input);
            foundations.Add(new{engine=EngineLibrary.Names[family],bytes=Convert.ToHexString(program),input,trace,
                joints=JointCensus.Read(trace,true)});
        }
        Files.Save(Path.Combine(folder,"foundations.json"),foundations);
        var files=new[]{"blind.csv.gz","contrasts.csv","exhibits.json","foundations.json","joints.json","joint-uses.csv.gz","route-activity.csv","replay-inputs.json","replay-expected.json"};
        Files.Save(Path.Combine(folder,"blind.lock"),new{ cases=rows.Length,finalists=Files.Digest(Path.Combine(folder,"finalists.json")),
            files=files.ToDictionary(p=>p,p=>Files.Digest(Path.Combine(folder,p))) });
    }
}
