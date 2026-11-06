using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentTension;

internal sealed record Lock(string Seal,Winner[] Winners,PilotResult[] Panels);
internal sealed record Job(string Id,Recipe Recipe,PathInput Input,int Slots,FeedbackMode Mode);
internal sealed record Seal(string Host,string Strokes,string Laws,Dictionary<string,string> Design);
internal static class Program
{
    private static readonly string Source=Path.Combine("experiments","OrnamentTension");
    private static Seal ReadSeal(string folder)=>new(Files.Digest(typeof(Program).Assembly.Location),
        Files.Digest(typeof(PathMachine).Assembly.Location),
        Files.Digest(typeof(Gojo.Experiments.PrimitiveMeasure.BitRelation).Assembly.Location),
        Directory.GetFiles(Path.Combine(folder,"design")).OrderBy(p=>Path.GetFileName(p),StringComparer.Ordinal)
            .ToDictionary(p=>Path.GetFileName(p),Files.Digest));

    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args is ["replay",var inputs,var outputs])
        {
            if(File.Exists(outputs))throw new IOException("Use a new replay output.");
            var jobs=Files.Load<Job[]>(inputs);
            Files.Save(outputs,jobs.Select(j=>new { j.Id, digest=Files.DigestObject(TensionMachine.Run(
                Convert.FromHexString(j.Recipe.Path),j.Input,j.Slots,Convert.FromHexString(j.Recipe.Policy),j.Mode,true)) }));
            return 0;
        }
        if(args.Length is <2 or >3)throw new ArgumentException("freeze/train/assay RUN [workers]; replay JOBS OUTPUT");
        var folder=Path.GetFullPath(args[1]);
        int workers=args.Length==3 ? int.Parse(args[2]) : Math.Clamp(Environment.ProcessorCount-2,1,8);
        if(workers is <1 or >64)throw new ArgumentOutOfRangeException(nameof(workers));
        if(args[0]=="freeze") {Freeze(folder);return 0;}
        if(Files.DigestObject(ReadSeal(folder))!=Files.DigestObject(Files.Load<Seal>(Path.Combine(folder,"seal.json"))))
            throw new InvalidDataException("Sealed runtime or design changed.");
        using var exclusive=new FileStream(Path.Combine(folder,".running"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if(args[0]=="train")Train(folder,workers);
        else if(args[0]=="assay")Assay.Run(folder,workers);
        else throw new ArgumentException("Unknown phase");
        return 0;
    }
    private static void Freeze(string folder)
    {
        if(Directory.Exists(folder)&&Directory.EnumerateFileSystemEntries(folder).Any())throw new IOException("Freeze in an empty directory.");
        string design=Path.Combine(folder,"design");Directory.CreateDirectory(design);
        foreach(string doc in new[]{"Protocol.md","Runtime.md","Engines.md","Criteria.md"})
            File.Copy(Path.Combine(Source,doc),Path.Combine(design,doc));
        Files.Save(Path.Combine(design,"settings.json"),new { Design.Bases, Design.Arms,Design.Slots,Design.Generations,
            Design.Population,Design.Replicates,Design.Pilot, engines=EngineLibrary.Entries,judges=TensionCritics.Names,
            train=Enumerable.Range(0,Design.Replicates).Select(rep=>Design.Settings("train",rep)),
            meta=Enumerable.Range(0,Design.Replicates).Select(rep=>Design.Settings("meta",rep)),
            validation=Enumerable.Range(0,Design.Replicates).Select(rep=>Design.Settings("validation",rep)),
            blind=Design.Settings("blind"),reserve=Design.Settings("reserve")});
        using(var f=File.CreateText(Path.Combine(design,"predictions.csv")))
        {
            f.WriteLine("family,slots,contrast,direction,margin");
            foreach(var family in EngineLibrary.Names)foreach(int slots in Design.Slots)foreach(string contrast in Design.Contrasts)
                f.WriteLine($"{family},{slots},{contrast},{Assay.Prediction(contrast)},{Assay.Margin(contrast)}");
        }
        Files.Save(Path.Combine(folder,"seal.json"),ReadSeal(folder));
        Console.WriteLine($"Seal {Files.Digest(Path.Combine(folder,"seal.json"))}");
    }
    private static void Train(string folder,int workers)
    {
        if(File.Exists(Path.Combine(folder,"finalists.lock")))throw new IOException("Finalists already locked.");
        var timer=Stopwatch.StartNew();
        var options=new ParallelOptions{MaxDegreeOfParallelism=workers};
        var pilots=new PilotResult[EngineLibrary.Names.Length*Design.Replicates];
        Parallel.For(0,pilots.Length,options,i=>{
            string f=Path.Combine(folder,"pilots",$"{i}.json");
            if(File.Exists(f))pilots[i]=Files.Load<PilotResult>(f);
            else{pilots[i]=Search.Nominate(i/Design.Replicates,i%Design.Replicates,folder);Files.Save(f,pilots[i]);}
        });
        var islands=new Island[pilots.Length*Design.Arms.Length];var sync=new object();int completed=0;
        Parallel.For(0,islands.Length,options,i=>{
            var pilot=pilots[i/Design.Arms.Length];string arm=Design.Arms[i%Design.Arms.Length];
            string f=Path.Combine(folder,"islands",$"{pilot.Family}-{arm}-{pilot.Replicate}.json");
            if(File.Exists(f))
            {
                islands[i]=Files.Load<Island>(f);
                if(Files.Digest(Path.Combine(folder,"histories",$"{pilot.Family}-{arm}-{pilot.Replicate}.csv.gz"))!=islands[i].HistoryHash)
                    throw new InvalidDataException("History differs from island record");
            }
            else{islands[i]=Search.Train(folder,pilot.Family,pilot.Replicate,arm,pilot);Files.Save(f,islands[i]);}
            lock(sync){if(++completed%8==0)Console.WriteLine($"{DateTime.UtcNow:T} {completed}/{islands.Length}, {timer.Elapsed.TotalSeconds:F1} seconds");}
        });
        string path=Path.Combine(folder,"finalists.json");
        Files.Save(path,new Lock(Files.Digest(Path.Combine(folder,"seal.json")),islands.Select(x=>x.Winner).ToArray(),pilots));
        Files.Save(Path.Combine(folder,"finalists.lock"),new{sha256=Files.Digest(path),workers,
            seconds=timer.Elapsed.TotalSeconds,completed=DateTime.UtcNow.ToString("O")});
        Console.WriteLine($"Locked {islands.Length} winners.");
    }
    public static Lock Frozen(string folder)
    {
        string path=Path.Combine(folder,"finalists.json");
        if(Files.Load<JsonElement>(Path.Combine(folder,"finalists.lock")).GetProperty("sha256").GetString()!=Files.Digest(path))
            throw new InvalidDataException("Finalists lock differs.");
        var result=Files.Load<Lock>(path);
        if(result.Seal!=Files.Digest(Path.Combine(folder,"seal.json")) ||
            result.Winners.Length!=EngineLibrary.Names.Length*Design.Replicates*Design.Arms.Length)
            throw new InvalidDataException("Finalists incomplete.");
        return result;
    }
}
