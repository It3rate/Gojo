using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentPlateStudy;

internal sealed record Candidate(int Id,string Path,string Policy,double Score,double Historical,double Generic);
internal sealed record Winner(string Arm,int Rep,Candidate Selected,double Validation,string LedgerHash);
internal sealed record Scene(int Parent,int Slots,PathInput Input);
internal static partial class Program
{
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
    private static readonly string[] Arms=["printed-start","library-history","library-generic"];
    private const string Project="experiments/OrnamentPlateStudy";
    private static string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
    private static void Write<T>(string file,T value)
    {
        string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temp,JsonSerializer.Serialize(value,Json));File.Move(temp,file,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    private static T Read<T>(string file)=>JsonSerializer.Deserialize<T>(File.ReadAllText(file),Json)!;
    private static Dictionary<string,string> Identity()=>new()
    {
        ["protocol"]=Hash(Path.Combine(Project,"Protocol.md")),
        ["host"]=Hash(typeof(Program).Assembly.Location),
        ["tension"]=Hash(typeof(TensionMachine).Assembly.Location),
        ["vector"]=Hash(typeof(PathMachine).Assembly.Location),
        ["odv"]=Hash(typeof(Gojo.Experiments.PrimitiveMeasure.BitRelation).Assembly.Location)
    };
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args.Length<2)throw new ArgumentException("freeze|train RUN or assay RUN RELEASE");
        string run=Path.GetFullPath(args[1]);
        if(args[0]=="freeze")
        {
            if(Directory.Exists(run) && Directory.EnumerateFileSystemEntries(run).Any())throw new IOException("Use a new run.");
            Directory.CreateDirectory(run);
            File.Copy(Path.Combine(Project,"Protocol.md"),Path.Combine(run,"Protocol.md"));
            Write(Path.Combine(run,"seal.json"),Identity());return;
        }
        var seal=Read<Dictionary<string,string>>(Path.Combine(run,"seal.json"));
        if(Identity().Any(kv=>kv.Value!=seal[kv.Key])||Hash(Path.Combine(run,"Protocol.md"))!=seal["protocol"])
            throw new InvalidDataException("Registered study changed.");
        using var gate=new FileStream(Path.Combine(run,".run-lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if(args[0]=="train")Train(run);
        else if(args.Length==3 && args[0]=="assay")Assay(run,args[2]);
        else throw new ArgumentException("Unknown phase or missing release.");
    }

    private static Scene[] Cases(string split,int rep=0,int figure=0)
    {
        (int a,int b,int p)[] bank=split switch{
            "train"=>[(2,1,3),(3,1,5),(4,2,6),(4,3,7)],
            "validation"=>[(6,3,9),(2,3,6),(5,2,8),(3,2,5)],
            "final"=>Enumerable.Range(0,12).Select(i=>
            {
                int b=4+i%4,delta=new[]{-2,-1,1,2}[i%4];
                return (2*b+(i>=4 && i<8 ? delta : 0),b,((figure==0 ? 3 : 4)*b)+(i>=8 ? delta : 0));
            }).ToArray(),
            _=>throw new ArgumentException(split)
        };
        var cases=new List<Scene>();
        for(int i=0;i<bank.Length;i++)
        {
            var (a,b,p)=bank[i];
            int[] slots=split=="final" ? [3,9] : [(i+rep)%2==0 ? 3 : 9];
            foreach(int slot in slots)cases.Add(new(i,slot,new(a,b,p,4,(rep+i)%4,(i+rep)%2)));
        }
        return cases.ToArray();
    }
    private static Candidate Evaluate(byte[] path,byte[] policy,int id,Scene[] bank,string arm)
    {
        double hist=0,generic=0;
        foreach(var test in bank)
        {
            var tr=TensionMachine.Run(path,test.Input,test.Slots,policy).Trace;
            var s=PlateReads.Measure(tr,0);
            hist+=s.Historical/bank.Length;generic+=s.General/bank.Length;
        }
        return new(id,Convert.ToHexString(path),Convert.ToHexString(policy),arm=="library-generic" ? generic : hist,hist,generic);
    }
    private static Winner TrainIsland(string run,string arm,int rep)
    {
        string ledger=Path.Combine(run,$"histories/{arm}-{rep}.csv.gz");
        string file=Path.Combine(run,$"islands/{arm}-{rep}.json");
        if(File.Exists(file))
        {
            var old=Read<Winner>(file);if(old.LedgerHash!=Hash(ledger))throw new IOException("Damaged history.");
            return old;
        }
        Candidate[] elite=[];
        var training=Cases("train",rep);var validation=Cases("validation",rep);
        using(var rows=new StreamWriter(new GZipStream(File.Create(ledger),CompressionLevel.Fastest)))
        {
            rows.WriteLine("id,generation,parent,path,policy,training,historical,generic");
            for(int g=0;g<32;g++)
            {
                var fresh=new List<Candidate>();
                for(int j=0;j<16;j++)
                {
                    int id=g*16+j;
                    var rng=new Random(208981+rep*78691+id*104729);
                    byte[] path,policy;int parent=-1;
                    if(g==0 || j%8==0)
                    {
                        path=arm=="printed-start" ? PlateReads.Author(0) : EngineLibrary.Create(rng.Next(8));
                        int changes=id==0 ? 0 : rng.Next(1,5);
                        for(int k=0;k<changes;k++)path=EngineLibrary.Mutate(path,rng);
                        policy=id==0 ? TensionMachine.DefaultPolicy() : TensionMachine.RandomPolicy(rng);
                    }
                    else
                    {
                        var source=elite[rng.Next(elite.Length)];
                        parent=source.Id;path=Convert.FromHexString(source.Path);
                        policy=Convert.FromHexString(source.Policy);
                        if(j%3==0)policy=TensionMachine.MutatePolicy(policy,rng);else path=EngineLibrary.Mutate(path,rng);
                    }
                    var candidate=Evaluate(path,policy,id,training,arm);
                    rows.WriteLine($"{id},{g},{parent},{candidate.Path},{candidate.Policy},{candidate.Score:R},{candidate.Historical:R},{candidate.Generic:R}");
                    fresh.Add(candidate);
                }
                elite=elite.Concat(fresh).OrderByDescending(c=>c.Score).ThenBy(c=>c.Path.Length).ThenBy(c=>c.Id)
                    .DistinctBy(c=>(c.Path,c.Policy)).Take(8).ToArray();
            }
        }
        var choice=elite.Take(6).Select(c=>(Source:c,Score:Evaluate(Convert.FromHexString(c.Path),
            Convert.FromHexString(c.Policy),c.Id,validation,arm).Score))
            .OrderByDescending(c=>c.Score).ThenByDescending(c=>c.Source.Score).ThenBy(c=>c.Source.Id).First();
        var winner=new Winner(arm,rep,choice.Source,choice.Score,Hash(ledger));
        Write(file,winner);return winner;
    }
    private static void Train(string run)
    {
        if(File.Exists(Path.Combine(run,"lock.json")))throw new IOException("Finalists locked.");
        Directory.CreateDirectory(Path.Combine(run,"histories"));Directory.CreateDirectory(Path.Combine(run,"islands"));
        var jobs=(from a in Arms from r in Enumerable.Range(0,4) select (a,r)).ToArray();
        var winners=new Winner[jobs.Length];
        Parallel.For(0,jobs.Length,new ParallelOptions{MaxDegreeOfParallelism=6},i=>
            winners[i]=TrainIsland(run,jobs[i].a,jobs[i].r));
        string file=Path.Combine(run,"finalists.json");
        Write(file,winners);
        Write(Path.Combine(run,"lock.json"),new {seal=Hash(Path.Combine(run,"seal.json")),finalists=Hash(file),
            count=winners.Length,utc=DateTime.UtcNow});
        Console.WriteLine($"Locked {winners.Length} finalists {Hash(file)}");
    }
}
