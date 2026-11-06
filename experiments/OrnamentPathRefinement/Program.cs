using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentPathRefinement;

internal sealed record Scene(string Name,string Parent,PathInput Input);
internal sealed record Reading(double Original,double History,double Witness);
internal sealed record Trial(int Id,string Hex,double Value,Reading Reads);
internal sealed record Winner(string Brief,string Arm,int Rep,int Trial,string Hex,Reading Train,Reading Validation,int Distinct);
internal sealed record Seal(string ParentSeal,string Design,string Spec,string Runtime,string Kernel,string PrimaryRuntime);
internal static partial class Program
{
    internal const int Generations=64,Population=64,Shortlist=8,Replicates=4;
    internal static readonly string[] Briefs=["returning-key","ramified-spray"];
    internal static readonly string[] Arms=["baseline","blend","history"];
    internal static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
    internal static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    internal static void Save<T>(string file,T item)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        string tmp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {File.WriteAllText(tmp,JsonSerializer.Serialize(item,Json));File.Move(tmp,file,true);}
        finally {if(File.Exists(tmp))File.Delete(tmp);}
    }
    internal static T Read<T>(string file)=>JsonSerializer.Deserialize<T>(File.ReadAllText(file),Json)??throw new InvalidDataException(file);
    private static readonly string Project="experiments/OrnamentPathRefinement";
    private static readonly string Primary="artifacts/local-path-ornament/wave1";
    private static string Spec=>Path.Combine(Project,"Protocol.md");
    private static string WaveDesign=>Path.Combine(Primary,"design","design.json");
    internal static string F(double d)=>d.ToString("R",CultureInfo.InvariantCulture);
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args.Length<2)throw new ArgumentException("freeze RUN | train RUN [workers] | assay RUN RELEASE | verify RUN");
        string root=Path.GetFullPath(args[1]);
        if(args[0]=="freeze"){Freeze(root);return;}
        Verify(root);
        if(args[0]=="verify")return;
        using var exclusive=new FileStream(Path.Combine(root,".run-lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if(args[0]=="train"){Train(root,args.Length>=3?int.Parse(args[2]):4);return;}
        if(args[0]=="assay" && args.Length==3){Assay(root,args[2]);return;}
        throw new ArgumentException("Unknown phase or missing shared reserve release.");
    }
    private static Seal Identity()=>new(Hash(Path.Combine(Primary,"seal.json")),Hash(WaveDesign),Hash(Spec),
        Hash(typeof(Program).Assembly.Location),Hash(typeof(PathMachine).Assembly.Location),
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Primary,"seal.json"))).RootElement.GetProperty("host").GetString()!);
    private static void Freeze(string run)
    {
        if(Directory.Exists(run)&&Directory.EnumerateFileSystemEntries(run).Any())throw new IOException("Use an empty follow-up folder.");
        var id=Identity();
        if(id.Kernel!=id.PrimaryRuntime)throw new IOException("Original path runtime fingerprint differs.");
        Directory.CreateDirectory(run);
        File.Copy(Spec,Path.Combine(run,"Protocol.md"));
        File.Copy(WaveDesign,Path.Combine(run,"wave-design.json"));
        var challenges=Challenges.Run();
        if(challenges.Any(c=>!c.Pass))throw new InvalidDataException("Style reader challenge failed.");
        Save(Path.Combine(run,"challenges.json"),challenges);
        Save(Path.Combine(run,"seal.json"),id);
        Console.WriteLine($"Registered {Hash(Path.Combine(run,"seal.json"))}");
    }
    private static void Verify(string run)
    {
        var s=Read<Seal>(Path.Combine(run,"seal.json"));
        if(s!=Identity()||Hash(Path.Combine(run,"Protocol.md"))!=s.Spec||Hash(Path.Combine(run,"wave-design.json"))!=s.Design)
            throw new InvalidDataException("Refinement seal differs.");
    }

    private static double Score(Reading r,string arm)=>arm switch
    {"baseline"=>r.Original,"blend"=>(r.History+r.Original)*.5,"history"=>r.History,_=>throw new ArgumentOutOfRangeException(nameof(arm))};
    internal static string PrimaryBrief(string brief)=>brief=="returning-key"?"turning-key":"branched-spray";
    private static Reading Evaluate(string brief,PathTrace trace,double[] weights)
    {
        var metrics=PathJudge.Measure(trace);
        bool viable=trace.Edges.Length>=2&&trace.Edges.Any(e=>(e.Direction&3)!=(trace.Edges[0].Direction&3));
        if(!viable)return new(0,0,0);
        double original=0;
        for(int i=0;i<weights.Length;i++)if(double.IsFinite(metrics[i]))original+=metrics[i]*weights[i];
        var h=StyleJudge.Read(trace,brief,metrics[0]);
        return new(original,h.Utility,h.Witness);
    }
    private static Reading Evaluate(string brief,byte[] bytes,Scene[] cases,double[] weights)
    {
        double original=0,history=0,witness=0;
        foreach(var sc in cases)
        {
            var r=Evaluate(brief,PathMachine.Run(bytes,sc.Input),weights);
            original+=r.Original;history+=r.History;witness+=r.Witness;
        }
        return new(original/cases.Length,history/cases.Length,witness/cases.Length);
    }
    private static Trial[] Rank(IEnumerable<Trial> trials)=>
        trials.OrderByDescending(t=>t.Value).ThenBy(t=>t.Hex.Length).ThenBy(t=>t.Id).DistinctBy(t=>t.Hex).Take(Shortlist).ToArray();
    private static void Train(string run,int workers)
    {
        if(workers<1||workers>32)throw new ArgumentOutOfRangeException(nameof(workers));
        if(File.Exists(Path.Combine(run,"lock.json")))throw new IOException("Finalists already locked.");
        using var design=JsonDocument.Parse(File.ReadAllText(Path.Combine(run,"wave-design.json")));
        var root=design.RootElement;
        var jobs=(from b in Briefs from a in Arms from r in Enumerable.Range(0,Replicates)select (b,a,r)).ToArray();
        var results=new Winner[jobs.Length];
        string histories=Path.Combine(run,"histories");Directory.CreateDirectory(histories);
        int completed=0;
        Parallel.For(0,jobs.Length,new ParallelOptions{MaxDegreeOfParallelism=workers},i=>
        {
            var (brief,arm,rep)=jobs[i];
            var train=root.GetProperty("training")[rep].Deserialize<Scene[]>(Json)!;
            var validation=root.GetProperty("validation").Deserialize<Scene[]>(Json)!;
            var w=root.GetProperty("weights").GetProperty(PrimaryBrief(brief)).Deserialize<double[]>(Json)!;
            string file=Path.Combine(histories,$"{brief}-{arm}-{rep}.csv.gz"),summary=file+".json";
            if(File.Exists(summary))
            {
                var done=Read<Island>(summary);
                if(Hash(file)!=done.Hash)throw new InvalidDataException(summary);
                results[i]=done.Winner;
            }
            else
            {
                results[i]=RunIsland(brief,arm,rep,train,validation,w,file);
                Save(summary,new Island(Hash(file),results[i]));
            }
            Console.WriteLine($"{Interlocked.Increment(ref completed)}/{jobs.Length}: {brief} {arm} {rep}");
        });
        Save(Path.Combine(run,"finalists.json"),results);
        Save(Path.Combine(run,"lock.json"),new{seal=Hash(Path.Combine(run,"seal.json")),
            finalists=Hash(Path.Combine(run,"finalists.json")),count=results.Length,utc=DateTime.UtcNow});
    }
    private sealed record Island(string Hash,Winner Winner);
    private static Winner RunIsland(string brief,string arm,int rep,Scene[] train,Scene[] validation,double[] w,string file)
    {
        Trial[] elite=[];
        var distinct=new HashSet<string>();
        string pending=file+".pending";
        using(var z=new StreamWriter(new GZipStream(File.Create(pending),CompressionLevel.Fastest),new UTF8Encoding(false)))
        {
            z.WriteLine("id,generation,slot,parent_a,parent_b,edit,hex,original,history,witness,objective");
            for(int g=0;g<Generations;g++)
            {
                var children=new List<Trial>();
                for(int k=0;k<Population;k++)
                {
                    int id=g*Population+k;
                    var rng=new Random(774793+rep*70001+id*4289);
                    Trial? a=null,b=null; byte[] bytes; string edit;
                    if(g==0||k%8==0)
                    {
                        if(g==0&&k<6){bytes=PathMachine.Foundation(k);edit="foundation";}
                        else if(rng.Next(4)==0){bytes=PathMachine.CreateUnrestricted(rng);edit="free";}
                        else{bytes=PathMachine.Create(rng,rng.Next(6));edit="seed-variation";}
                    }
                    else
                    {
                        a=elite[rng.Next(elite.Length)];
                        if(k%5==0){b=elite[rng.Next(elite.Length)];bytes=PathMachine.Cross(Convert.FromHexString(a.Hex),Convert.FromHexString(b.Hex),rng);edit="exchange";}
                        else{bytes=PathMachine.Mutate(Convert.FromHexString(a.Hex),rng);edit="mutation";}
                    }
                    string hex=Convert.ToHexString(bytes); distinct.Add(hex);
                    var read=Evaluate(brief,bytes,train,w);
                    double s=Score(read,arm);
                    children.Add(new(id,hex,s,read));
                    z.WriteLine($"{id},{g},{k},{a?.Id},{b?.Id},{edit},{hex},{F(read.Original)},{F(read.History)},{F(read.Witness)},{F(s)}");
                }
                elite=Rank(elite.Concat(children));
            }
        }
        File.Move(pending,file,true);
        var chosen=Rank(elite.Select(e=>{var v=Evaluate(brief,Convert.FromHexString(e.Hex),validation,w);return e with{Value=Score(v,arm),Reads=v};}))[0];
        var source=elite.Single(e=>e.Id==chosen.Id);
        return new(brief,arm,rep,chosen.Id,chosen.Hex,source.Reads,chosen.Reads,distinct.Count);
    }
}
