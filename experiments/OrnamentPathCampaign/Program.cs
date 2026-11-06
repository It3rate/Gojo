using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gojo.Experiments.OrnamentPathCampaign;

internal sealed record Seal(string Host,string Kernel,Dictionary<string,string> Files,string RegisteredUtc);
internal sealed record Finalists(string Protocol, Winner[] Winners, Winner[] Foundations);
internal sealed record ReplayJob(string Id,string Hex,PathInput Input,PathMode Mode);
internal sealed record History(Island Island,string LedgerHash);

internal static class Program
{
    public static string TraceDigest(PathTrace trace) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(trace,Record.Json)));
    private static string Host => typeof(Program).Assembly.Location;
    private static string Kernel => typeof(Gojo.Experiments.PrimitiveMeasure.BitRelation).Assembly.Location;
    private static string Source => Path.Combine("experiments","OrnamentPathCampaign");

    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args is ["replay",var input,var output])
        {
            var jobs=Record.Read<ReplayJob[]>(input);
            Record.Write(output,jobs.Select(j=>new { j.Id, digest=TraceDigest(
                PathMachine.Run(Convert.FromHexString(j.Hex),j.Input,j.Mode)) }).ToArray());
            return 0;
        }
        if(args.Length is <2 or >3) throw new ArgumentException("freeze|train|assay RUN [workers], or replay INPUT OUTPUT");
        string folder=Path.GetFullPath(args[1]);
        int workers=args.Length==3 ? int.Parse(args[2]) : Math.Clamp(Environment.ProcessorCount-2,1,8);
        if(workers is <1 or >64) throw new ArgumentOutOfRangeException(nameof(workers));
        if(args[0]=="freeze") { Freeze(folder); return 0; }
        Check(folder);
        using var ownership=new FileStream(Path.Combine(folder,".running"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if(args[0]=="train") Train(folder,workers);
        else if(args[0]=="assay") Assay.Run(folder);
        else throw new ArgumentException("Unknown command.");
        return 0;
    }

    private static void Freeze(string folder)
    {
        if(Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new IOException("Use a new run directory.");
        var dir=Path.Combine(folder,"design");
        Directory.CreateDirectory(dir);
        foreach(string file in new[]{"Protocol.md","Criteria.md","Encoding.md"})
            File.Copy(Path.Combine(Source,file),Path.Combine(dir,file));
        Record.Write(Path.Combine(dir,"design.json"),new { Design.Replicates, Design.Generations, Design.Population,
            Design.Shortlist,Design.Briefs,Design.Arms,criteria=PathJudge.Criteria,
            weights=Design.Briefs.ToDictionary(b=>b.Name,b=>Design.Weights(b,"full")),
            training=Enumerable.Range(0,Design.Replicates).Select(r=>Design.Scenarios("train",r)),
            validation=Design.Scenarios("validation"),blind=Design.Scenarios("blind"),reserve=Design.Scenarios("reserve") });
        var predictions=new List<string>{"brief,contrast,stratum,predicted,margin"};
        foreach(var b in Design.Briefs) foreach(var arm in Design.Arms.Where(a=>a!="full").Concat(["foundation","cut","frozen","open"]))
            foreach(var stratum in new[]{"all","basis-A","basis-B","basis-C","basis-D"})
                predictions.Add(Record.Csv(b.Name,arm,stratum,Assay.Prediction(b,arm),.02));
        File.WriteAllLines(Path.Combine(dir,"predictions.csv"),predictions);
        var seal=new Seal(Record.Hash(Host),Record.Hash(Kernel),Directory.GetFiles(dir).ToDictionary(
            p=>Path.GetFileName(p),Record.Hash),DateTime.UtcNow.ToString("O"));
        Record.Write(Path.Combine(folder,"seal.json"),seal);
        Console.WriteLine($"Frozen {predictions.Count-1} comparisons. Seal {Record.Hash(Path.Combine(folder,"seal.json"))}");
    }
    private static void Check(string folder)
    {
        var seal=Record.Read<Seal>(Path.Combine(folder,"seal.json"));
        if(seal.Host!=Record.Hash(Host) || seal.Kernel!=Record.Hash(Kernel) ||
            seal.Files.Any(kv=>Record.Hash(Path.Combine(folder,"design",kv.Key))!=kv.Value))
            throw new InvalidDataException("The registered runtime or design changed.");
    }
    private static void Train(string folder,int workers)
    {
        string lockFile=Path.Combine(folder,"finalists.json");
        if(File.Exists(Path.Combine(folder,"lock.json"))) throw new IOException("Finalists already locked.");
        var watch=Stopwatch.StartNew();
        var jobs=(from brief in Design.Briefs from arm in Design.Arms from rep in Enumerable.Range(0,Design.Replicates)
            select (brief,arm,rep)).ToArray();
        var results=new History[jobs.Length];
        int done=0;
        var reportLock=new object();
        Parallel.For(0,jobs.Length,new ParallelOptions{MaxDegreeOfParallelism=workers},i=>{
            var (brief,arm,rep)=jobs[i];
            string stem=$"{brief.Name}_{arm}_{rep}";
            string path=Path.Combine(folder,"islands",stem+".json");
            string ledger=Path.Combine(folder,"histories",stem+".csv.gz");
            if(File.Exists(path))
            {
                results[i]=Record.Read<History>(path);
                if(Record.Hash(ledger)!=results[i].LedgerHash)throw new InvalidDataException(ledger);
            }
            else
            {
                var result=Competition.Train(folder,brief,arm,rep);
                results[i]=new(result,Record.Hash(ledger));
                Record.Write(path,results[i]);
            }
            lock(reportLock)
            {
                done++;
                if(done%10==0 || done==jobs.Length)
                    Console.WriteLine($"{DateTime.UtcNow:T} completed {done}/{jobs.Length} populations ({watch.Elapsed.TotalSeconds:F0}s)");
            }
        });
        var winners=results.Select(r=>r.Island.Winner).ToArray();
        using(var writer=File.CreateText(Path.Combine(folder,"criteria-utility.csv")))
        {
            writer.WriteLine("brief,key,group,weight,eligible_pairs,positive_pivots,negative_pivots");
            foreach(var b in Design.Briefs)
            {
                var pool=results.Select(r=>r.Island).Where(r=>r.Winner.Brief==b.Name && r.Winner.Arm=="full").ToArray();
                var weights=Design.Weights(b,"full");
                for(int i=0;i<weights.Length;i++)
                {
                    var c=PathJudge.Criteria[i];
                    writer.WriteLine(Record.Csv(b.Name,c.Key,c.Group,weights[i],pool.Sum(r=>r.Pivots.Eligible[i]),
                        pool.Sum(r=>r.Pivots.Positive[i]),pool.Sum(r=>r.Pivots.Negative[i])));
                }
            }
        }
        var foundations=Design.Briefs.Select(b=>{
            var weights=Design.Weights(b,"full");
            return Enumerable.Range(0,6).Select(f=>{
                var bytes=PathMachine.Foundation(f);
                double val=Competition.Score(Competition.Assess(bytes,Design.Scenarios("validation")),weights);
                return new Winner(b.Name,"foundation",0,Convert.ToHexString(bytes),f,0,val,6,6);
            }).OrderByDescending(w=>w.ValidationUtility).ThenBy(w=>w.Hex.Length).ThenBy(w=>w.Trial).First();
        }).ToArray();
        Record.Write(lockFile,new Finalists(Record.Hash(Path.Combine(folder,"seal.json")),winners,foundations));
        string fingerprint=Record.Hash(lockFile);
        Record.Write(Path.Combine(folder,"lock.json"),new { fingerprint,winners=winners.Length,workers,
            searchSeconds=watch.Elapsed.TotalSeconds,completedUtc=DateTime.UtcNow.ToString("O") });
        Console.WriteLine($"Finalists locked: {fingerprint}");
    }
    public static Finalists Locked(string folder)
    {
        string file=Path.Combine(folder,"finalists.json");
        var locked=Record.Read<JsonElement>(Path.Combine(folder,"lock.json"));
        var finalists=Record.Read<Finalists>(file);
        if (Record.Hash(file)!=locked.GetProperty("fingerprint").GetString() ||
            finalists.Protocol!=Record.Hash(Path.Combine(folder,"seal.json")) ||
            finalists.Winners.Length!=Design.Briefs.Length*Design.Arms.Length*Design.Replicates)
            throw new InvalidDataException("Finalist lock mismatch.");
        return finalists;
    }
}
