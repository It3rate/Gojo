using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentDensityResponse;

internal sealed record Setting(string Key,string Parent,int Slots,PathInput Input);
internal sealed record Seal(string Protocol,string Settings,string ParentSeal,string ParentFinalists,string Host,string Feedback,string Pen);
internal sealed record Candidate(int Position,string Policy,double Train,double Form,double Response);
internal sealed record Winner(int Family,int Replicate,string Arm,string Path,string Policy,int Trial,double Train,double Validation);
internal sealed record Island(Winner Winner,int Unique,string Ledger);
internal sealed record Measure(double Length,double Broad,double Course,double Reference,int Actions,int Relief,string Material,string History);
internal sealed record Triple(Measure Plus,Measure Minus,Measure Zero,double Response,double Gain)
{
    public double RequestedForm=>(Plus.Broad+Minus.Broad)/2;
}
internal static partial class Program
{
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
    private const string Primary="artifacts/local-tension-ornament/wave1";
    private const string Protocol="experiments/OrnamentDensityResponse/Protocol.md";
    private static readonly int[] Families=[0,1,4,5];
    private static readonly string[] Arms=["form","response"];
    private static string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
    private static string Digest<T>(T item)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(item,Json)));
    private static T Load<T>(string file)=>JsonSerializer.Deserialize<T>(File.ReadAllText(file),Json)??throw new InvalidDataException(file);
    private static void Save<T>(string path,T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try { File.WriteAllText(temp,JsonSerializer.Serialize(value,Json));File.Move(temp,path,true); }
        finally { if(File.Exists(temp))File.Delete(temp); }
    }
    private static string Number(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args.Length<2)throw new ArgumentException("freeze RUN | train RUN [workers] | assay RUN RELEASE | verify RUN");
        string dir=Path.GetFullPath(args[1]);
        if(args[0]=="freeze")
        {
            if(Directory.Exists(dir) && Directory.EnumerateFileSystemEntries(dir).Any())throw new IOException("Use a fresh run.");
            Directory.CreateDirectory(dir); File.Copy(Protocol,Path.Combine(dir,"Protocol.md"));
            File.Copy(Path.Combine(Primary,"design","settings.json"),Path.Combine(dir,"settings.json"));
            Save(Path.Combine(dir,"seal.json"),Identity());
            Verify(dir);Console.WriteLine($"Sealed {Hash(Path.Combine(dir,"seal.json"))}");return;
        }
        Verify(dir);
        using var exclusive=new FileStream(Path.Combine(dir,".running"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(dir,"settings.json")));
        if(args[0]=="train")Train(dir,doc.RootElement,args.Length>2 ? int.Parse(args[2]) : 6);
        else if(args[0]=="assay" && args.Length==3)Assay(dir,doc.RootElement,args[2]);
        else if(args[0]=="verify") { CheckWinners(dir);Console.WriteLine("Study and finalist seals verified."); }
        else throw new ArgumentException("Unknown phase or missing gate.");
    }

    private static Seal Identity()=>new(Hash(Protocol),Hash(Path.Combine(Primary,"design","settings.json")),
        Hash(Path.Combine(Primary,"seal.json")),Hash(Path.Combine(Primary,"finalists.json")),Hash(typeof(Program).Assembly.Location),
        Hash(typeof(TensionMachine).Assembly.Location),Hash(typeof(PathMachine).Assembly.Location));
    private static void Verify(string dir)
    {
        var seal=Load<Seal>(Path.Combine(dir,"seal.json"));
        var parent=Load<JsonElement>(Path.Combine(Primary,"seal.json"));
        if(seal!=Identity() || Hash(Path.Combine(dir,"settings.json"))!=seal.Settings || Hash(Path.Combine(dir,"Protocol.md"))!=seal.Protocol ||
           parent.GetProperty("host").GetString()!=seal.Feedback || parent.GetProperty("strokes").GetString()!=seal.Pen)
            throw new InvalidDataException("Source executable or registration changed.");
    }
    private static Winner[] CheckWinners(string dir)
    {
        string file=Path.Combine(dir,"finalists.json");
        if(Load<JsonElement>(Path.Combine(dir,"lock.json")).GetProperty("finalists").GetString()!=Hash(file))
            throw new InvalidDataException("Finalists changed.");
        var winners=Load<Winner[]>(file);
        if(winners.Length!=32 || winners.DistinctBy(w=>(w.Family,w.Replicate,w.Arm)).Count()!=32 ||
            winners.Any(w=>Convert.ToHexString(EngineLibrary.Create(w.Family))!=w.Path))throw new InvalidDataException("Wrong cohort");
        return winners;
    }
    private static Setting[] Settings(JsonElement doc,string group,int rep=0)
    {
        var node=doc.GetProperty(group);
        if(group is "train" or "validation")node=node[rep];
        return JsonSerializer.Deserialize<Setting[]>(node,Json)!;
    }

    private static Measure Draw(byte[] path,byte[] policy,int family,Setting s,int sign,string mode,bool holdout,bool record)
    {
        var state=mode switch { "quiet"=>FeedbackMode.Quiet,"fixed"=>FeedbackMode.Fixed,"cut-down"=>FeedbackMode.CutDown,
            "misrouted"=>FeedbackMode.Misrouted,_=>FeedbackMode.Live };
        int lane=mode=="turn-lane" ? 1 : 0;
        int owner=mode=="direct" ? 0 : path[1]+1;
        int half=PathMachine.Decode(path).Max(o=>o.Rows.Length)*s.Input.Cells/2;
        PressureImpulse[] pulses=sign==0 ? [] : [new(0,owner,lane,sign*3),new(half,owner,lane,sign*3)];
        var result=TensionMachine.Run(path,s.Input,s.Slots,policy,state,false,pulses);
        var read=TensionCritics.Measure(result.Trace,s.Slots,EngineLibrary.Names[family],holdout);
        double deposited=0;
        HashSet<(int X,int Y,int D)>? atoms=record ? [] : null;
        foreach(var e in result.Trace.Edges)
        {
            var a=result.Trace.Sites[e.From];var b=result.Trace.Sites[e.To];
            int len=Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));
            deposited+=len*((e.Direction&1)==0 ? 1 : Math.Sqrt(2));
            if(atoms==null)continue;
            if(a.X>b.X || (a.X==b.X && a.Y>b.Y))(a,b)=(b,a);
            int dx=Math.Sign(b.X-a.X),dy=Math.Sign(b.Y-a.Y);
            for(int j=0;j<len;j++)atoms.Add((a.X+j*dx,a.Y+j*dy,(dx+1)*3+dy+1));
        }
        double unique=deposited*read.Raw["no-overdraw"];
        if(atoms!=null)
        {
            double independent=atoms.Sum(p=>p.D is 6 or 8 ? Math.Sqrt(2) : 1);
            if(Math.Abs(independent-unique)>1e-6)throw new InvalidDataException("Interval conservation mismatch.");
        }
        return new(unique,read.Broad,read.Raw["connected-course"],read.Reference,result.Stats.Actuations,result.Stats.Relief,
            atoms==null ? "" : Digest(atoms.Order().Select(p=>new[]{p.X,p.Y,p.D}).ToArray()),record ? Digest(result.Trace) : "");
    }
    private static Triple Trial(byte[] path,byte[] policy,int family,Setting s,string mode,bool heldout=false,bool record=false)
    {
        var plus=Draw(path,policy,family,s,1,mode,heldout,record);
        var minus=Draw(path,policy,family,s,-1,mode,heldout,record);
        var zero=Draw(path,policy,family,s,0,mode,heldout,record);
        double gain=(plus.Length-minus.Length)/s.Input.Pitch;
        return new(plus,minus,zero,Math.Clamp(.5+.5*gain,0,1),gain);
    }
    private static Candidate Evaluate(int pos,byte[] policy,byte[] path,int family,string arm,Setting[] bank)
    {
        double form=0,requested=0,response=0;
        foreach(var s in bank)
        {
            var result=Trial(path,policy,family,s,"live");
            form+=result.Zero.Broad;requested+=result.RequestedForm;response+=result.Response;
        }
        double score=arm=="form" ? form/bank.Length : (.55*requested+.45*response)/bank.Length;
        return new(pos,Convert.ToHexString(policy),score,form/bank.Length,response/bank.Length);
    }
    private static Island Search(string dir,int family,int rep,string arm,Setting[] train,Setting[] val)
    {
        var path=EngineLibrary.Create(family);
        Candidate[] elites=[];var distinct=new HashSet<string>();
        string file=Path.Combine(dir,"histories",$"{family}-{arm}-{rep}.csv.gz");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using(var f=new StreamWriter(new GZipStream(File.Create(file),CompressionLevel.Fastest)))
        {
            f.WriteLine("position,parent,operator,policy,score,undisturbed_form,response");
            for(int g=0;g<32;g++)
            {
                var generation=new List<Candidate>();
                for(int k=0;k<16;k++)
                {
                    int pos=g*16+k;
                    var rng=new Random(455639+rep*7549+pos*18671);
                    byte[] policy;Candidate? parent=null;
                    if(pos==0)policy=TensionMachine.DefaultPolicy();
                    else if(g==0 || k%4==0)policy=TensionMachine.RandomPolicy(rng);
                    else { parent=elites[rng.Next(elites.Length)];policy=TensionMachine.MutatePolicy(Convert.FromHexString(parent.Policy),rng); }
                    var c=Evaluate(pos,policy,path,family,arm,train);
                    distinct.Add(c.Policy);generation.Add(c);
                    f.WriteLine($"{pos},{parent?.Position},{(parent==null ? "fresh" : "mutate")},{c.Policy},{Number(c.Train)},{Number(c.Form)},{Number(c.Response)}");
                }
                elites=elites.Concat(generation).OrderByDescending(c=>c.Train).ThenBy(c=>c.Position).DistinctBy(c=>c.Policy).Take(8).ToArray();
            }
        }
        var best=elites.Take(6).Select(c=>(c,validation:Evaluate(c.Position,Convert.FromHexString(c.Policy),path,family,arm,val).Train))
            .OrderByDescending(w=>w.validation).ThenByDescending(w=>w.c.Train).ThenBy(w=>w.c.Position).First();
        return new(new(family,rep,arm,Convert.ToHexString(path),best.c.Policy,best.c.Position,best.c.Train,best.validation),distinct.Count,Hash(file));
    }
    private static void Train(string dir,JsonElement doc,int workers)
    {
        if(workers is <1 or >64 || File.Exists(Path.Combine(dir,"lock.json")))throw new InvalidOperationException();
        var jobs=(from f in Families from a in Arms from r in Enumerable.Range(0,4) select(f,a,r)).ToArray();
        var winners=new Winner[jobs.Length];
        Parallel.For(0,jobs.Length,new ParallelOptions{MaxDegreeOfParallelism=workers},i=>{
            var (f,a,r)=jobs[i];string id=$"{f}-{a}-{r}";
            string file=Path.Combine(dir,"islands",id+".json");
            var island=File.Exists(file)?Load<Island>(file):Search(dir,f,r,a,Settings(doc,"train",r),Settings(doc,"validation",r));
            if(Hash(Path.Combine(dir,"histories",id+".csv.gz"))!=island.Ledger)throw new InvalidDataException(id);
            if(!File.Exists(file))Save(file,island);
            winners[i]=island.Winner;
        });
        var target=Path.Combine(dir,"finalists.json");Save(target,winners);
        Save(Path.Combine(dir,"lock.json"),new{finalists=Hash(target),seal=Hash(Path.Combine(dir,"seal.json")),
            count=winners.Length,utc=DateTime.UtcNow});
        Console.WriteLine($"Finalist lock {Hash(target)}");
    }
}
