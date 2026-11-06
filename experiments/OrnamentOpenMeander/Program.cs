using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentPathRefinement;

namespace Gojo.Experiments.OrnamentOpenMeander;

internal readonly record struct Scores(double Original,double Prior,double Enhanced,double Strong,double Through)
{
    public double Goal(string arm)=>(Original+(arm=="old-blend"?Prior:Enhanced))*.5;
    public static Scores operator +(Scores a,Scores b)=>new(a.Original+b.Original,a.Prior+b.Prior,a.Enhanced+b.Enhanced,a.Strong+b.Strong,a.Through+b.Through);
    public static Scores operator /(Scores s,int n)=>new(s.Original/n,s.Prior/n,s.Enhanced/n,s.Strong/n,s.Through/n);
}
internal sealed record Trial(int Id,string Hex,Scores Data,double Goal);
internal sealed record Champion(string Arm,int Rep,int Id,string Hex,Scores Train,Scores Validation,int Distinct);
internal static partial class Program
{
    private const string Source="experiments/OrnamentOpenMeander";
    private const string Primary="artifacts/local-path-ornament/wave1";
    private const string Previous="artifacts/local-path-ornament/shape-refinement";
    internal static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
    private static string Hash(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
    private static void Save<T>(string f,T v)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(f))!);
        string tmp=f+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(tmp,JsonSerializer.Serialize(v,Json));File.Move(tmp,f,true);}
        finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args.Length<2)throw new ArgumentException("freeze RUN | train RUN [workers] | assay RUN RELEASE");
        string dir=Path.GetFullPath(args[1]);
        if(args[0]=="freeze"){Freeze(dir);return;}
        var seal=JsonSerializer.Deserialize<Seal>(File.ReadAllText(Path.Combine(dir,"seal.json")),Json)!;
        if(seal!=Identity(dir))throw new InvalidDataException("Freeze fingerprints differ.");
        using var exclusive=new FileStream(Path.Combine(dir,".run-lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var cohort=JsonSerializer.Deserialize<Cohort>(File.ReadAllText(Path.Combine(dir,"cohort.json")),Json)!;
        if(args[0]=="train")Train(dir,cohort,args.Length==3?int.Parse(args[2]):4);
        else if(args[0]=="assay"&&args.Length==3)Assay(dir,cohort,args[2]);
        else throw new ArgumentException("Unknown phase or missing release file.");
    }
    private sealed record Seal(string Primary,string Kernel,string ExpectedKernel,string Runtime,string ParentOutcomes,string ParentAudit,string Spec,string Cohort);
    private static Seal Identity(string dir)=>new(Hash(Path.Combine(Primary,"seal.json")),Hash(typeof(PathMachine).Assembly.Location),
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Primary,"seal.json"))).RootElement.GetProperty("host").GetString()!,
        Hash(typeof(Program).Assembly.Location),Hash(Path.Combine(Previous,"reserve","complete.json")),Hash(Path.Combine(Previous,"diagnostics","winding.json")),
        Hash(Path.Combine(dir,"Protocol.md")),Hash(Path.Combine(dir,"cohort.json")));
    private static void Freeze(string dir)
    {
        if(Directory.Exists(dir)&&Directory.EnumerateFileSystemEntries(dir).Any())throw new IOException("Fresh run required.");
        Directory.CreateDirectory(dir);File.Copy(Path.Combine(Source,"Protocol.md"),Path.Combine(dir,"Protocol.md"));
        var cohort=Design.Enroll(Path.Combine(Primary,"design","design.json"));Save(Path.Combine(dir,"cohort.json"),cohort);
        var seal=Identity(dir);
        if(seal.Kernel!=seal.ExpectedKernel)throw new InvalidDataException("Use original path runtime.");
        var basis=cohort.Train[0].First(s=>s.Input.Pitch==s.Input.A+2*s.Input.B);
        var good=Measure(PathMachine.Run(Design.Reference(),basis.Input),cohort.Weights);
        var box=Measure(PathMachine.Run(Design.Box(),basis.Input),cohort.Weights);
        if(good.Strong!=1||good.Through!=1||box.Prior<=0||box.Strong!=0)throw new InvalidDataException($"Witness controls failed {good} / {box}.");
        Save(Path.Combine(dir,"challenges.json"),new{basis,positive=good,box,reference=Convert.ToHexString(Design.Reference()),boxProgram=Convert.ToHexString(Design.Box())});
        Save(Path.Combine(dir,"seal.json"),seal);Console.WriteLine("Registered "+Hash(Path.Combine(dir,"seal.json")));
    }
    private static Scores Measure(PathTrace t,double[] weights)
    {
        var values=PathJudge.Measure(t);
        if(t.Edges.Length<2||!t.Edges.Any(e=>(e.Direction&3)!=(t.Edges[0].Direction&3)))return default;
        double orig=0;for(int i=0;i<values.Length;i++)if(double.IsFinite(values[i]))orig+=values[i]*weights[i];
        var old=StyleJudge.Read(t,"returning-key",values[0]);
        var open=OpenJudge.Read(t,values[0]);
        return new(orig,old.Utility,open.Utility,open.Open,open.Through);
    }
    private static Scores Eval(byte[] b,Scene[] cases,double[] weights)
    {
        Scores sum=default;foreach(var s in cases)sum+=Measure(PathMachine.Run(b,s.Input),weights);
        return sum/cases.Length;
    }
    private static Trial[] Rank(IEnumerable<Trial> src)=>src.OrderByDescending(t=>t.Goal).ThenBy(t=>t.Hex.Length).ThenBy(t=>t.Id).DistinctBy(t=>t.Hex).Take(8).ToArray();
    private sealed record Island(string History,Champion Winner);
    private static void Train(string dir,Cohort c,int workers)
    {
        if(File.Exists(Path.Combine(dir,"lock.json")))throw new IOException("Already locked.");
        var jobs=(from a in new[]{"old-blend","open-blend"} from r in Enumerable.Range(0,4) select(a,r)).ToArray();
        var results=new Champion[jobs.Length];Directory.CreateDirectory(Path.Combine(dir,"histories"));
        Parallel.For(0,jobs.Length,new ParallelOptions{MaxDegreeOfParallelism=workers},i=>{
            var (arm,rep)=jobs[i];
            string file=Path.Combine(dir,"histories",$"{arm}-{rep}.csv.gz"),meta=file+".json";
            if(File.Exists(meta)){var rec=JsonSerializer.Deserialize<Island>(File.ReadAllText(meta),Json)!;
                if(rec.History!=Hash(file))throw new InvalidDataException(meta);results[i]=rec.Winner;return;}
            Trial[] elite=[];var seen=new HashSet<string>();
            using(var f=new StreamWriter(new GZipStream(File.Create(file+".pending"),CompressionLevel.Fastest),new UTF8Encoding(false)))
            {
                f.WriteLine("id,parent_a,parent_b,op,hex,original,prior,enhanced,strong,through,fitness");
                for(int g=0;g<64;g++)
                {
                    var generation=new List<Trial>();
                    for(int k=0;k<64;k++)
                    {
                        int id=g*64+k;var rng=new Random(831741+73331*rep+3457*id);
                        Trial? a=null,b=null;byte[] bytes;string op;
                        if(g==0||k%8==0)
                        {
                            if(g==0&&k<6){bytes=PathMachine.Foundation(k);op="foundation";}
                            else if(rng.Next(4)==0){bytes=PathMachine.CreateUnrestricted(rng);op="free";}
                            else {bytes=PathMachine.Create(rng,rng.Next(6));op="variation";}
                        }
                        else
                        {
                            a=elite[rng.Next(elite.Length)];
                            if(k%5==0){b=elite[rng.Next(elite.Length)];bytes=PathMachine.Cross(Convert.FromHexString(a.Hex),Convert.FromHexString(b.Hex),rng);op="exchange";}
                            else{bytes=PathMachine.Mutate(Convert.FromHexString(a.Hex),rng);op="mutation";}
                        }
                        var scores=Eval(bytes,c.Train[rep],c.Weights);string hex=Convert.ToHexString(bytes);seen.Add(hex);
                        var t=new Trial(id,hex,scores,scores.Goal(arm));generation.Add(t);
                        f.WriteLine($"{id},{a?.Id},{b?.Id},{op},{hex},{scores.Original:R},{scores.Prior:R},{scores.Enhanced:R},{scores.Strong:R},{scores.Through:R},{t.Goal:R}");
                    }
                    elite=Rank(elite.Concat(generation));
                }
            }
            File.Move(file+".pending",file,true);
            var w=Rank(elite.Select(t=>{var v=Eval(Convert.FromHexString(t.Hex),c.Validation,c.Weights);return t with{Data=v,Goal=v.Goal(arm)};}))[0];
            var champion=new Champion(arm,rep,w.Id,w.Hex,elite.Single(t=>t.Id==w.Id).Data,w.Data,seen.Count);
            results[i]=champion;Save(meta,new Island(Hash(file),champion));Console.WriteLine($"{arm} replicate {rep} locked");
        });
        Save(Path.Combine(dir,"finalists.json"),results);
        Save(Path.Combine(dir,"lock.json"),new{finalists=Hash(Path.Combine(dir,"finalists.json")),count=results.Length,seal=Hash(Path.Combine(dir,"seal.json")),utc=DateTime.UtcNow});
    }
}
