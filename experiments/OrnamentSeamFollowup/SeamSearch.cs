using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.OrnamentSeamFollowup;

internal static partial class SeamSearch
{
    private const int Replicates=4, Proposals=4096, TrainCount=32, Shortlist=8, Seed=620261126;
    private static readonly string[] Cues = ["original","seam","decoy"];
    private sealed record Seal(string Format,string SpecHash,string PrimaryFreezeHash,string PrimaryAssayHash,
        string RuntimeHash,string KernelHash,int Replicates,int Proposals,int TrainingEpisodes,int Shortlist,int Seed);
    private sealed record Candidate(int Id,string GenomeHex,TournamentMerit[] Training);
    private sealed record Pool(int Replicate,string TrainingHash,string PoolHash,Candidate[] Candidates);
    private sealed record Winner(int Replicate,string Cue,int Id,string GenomeHex,TournamentMerit Training,TournamentMerit Validation);
    private sealed record Finalist(int Replicate,string Cue,Candidate Candidate,TournamentMerit Validation);
    private sealed record Locked(string SealHash,string[] TrainingHashes,Winner[] Winners,string WinnerHash);
    private sealed record Result(int Utility,int Error,int Violations,bool Exact,ulong Ink,int Edits,int Attempts,int Blocked,int Stock);

    internal static void Freeze(string folder,string primary,string specPath)
    {
        if (!File.Exists(specPath)) throw new FileNotFoundException("Prospective protocol required.",specPath);
        var specCopy=Path.Combine(folder,"Protocol.md");
        if (Directory.Exists(folder))
        {
            var entries=Directory.GetFileSystemEntries(folder);
            // The protocol copy can be left behind by an interrupted seal. Only
            // an identical copy can resume. Never overwrite a completed seal.
            if(entries.Length!=0 && (entries.Length!=1 || entries[0]!=specCopy || !File.Exists(specCopy) ||
                HashFile(specCopy)!=HashFile(specPath)))
                throw new InvalidOperationException("Freeze requires an empty directory or its identical unfinished protocol copy.");
        }
        Directory.CreateDirectory(folder);
        if(!File.Exists(specCopy)) File.Copy(specPath,specCopy);
        var seal=Identity(folder,primary);
        Save(Path.Combine(folder,"seal.json"),seal);
        Console.WriteLine("Sealed seam-cue follow-up "+Hash(seal));
    }

    private static Seal Identity(string folder,string primary)
    {
        var kernelHash=HashFile(typeof(OrnamentProgram).Assembly.Location);
        // The primary CLI's JSON writer emits a UTF-8 preamble. Decode it for
        // the field read only; every file digest still covers its original bytes.
        using var primarySeal=JsonDocument.Parse(File.ReadAllText(Path.Combine(primary,"protocol.lock.json")));
        if(primarySeal.RootElement.GetProperty("RuntimeHash").GetString()!=kernelHash)
            throw new InvalidDataException("The kernel differs from the primary's sealed runtime.");
        return new("seam-cue-v1",HashFile(Path.Combine(folder,"Protocol.md")), HashFile(Path.Combine(primary,"frozen.json")),
            HashFile(Path.Combine(primary,"test","complete.json")),HashFile(typeof(SeamSearch).Assembly.Location),
            kernelHash,Replicates,Proposals,TrainCount,Shortlist,Seed);
    }

    internal static void Execute(string command,string folder,string primary,int workers)
    {
        if (workers < 1 || workers > 64) throw new ArgumentOutOfRangeException(nameof(workers));
        var seal=Read<Seal>(Path.Combine(folder,"seal.json"));
        if (seal != Identity(folder,primary)) throw new InvalidDataException("Sealed protocol or runtime differs.");
        using var lease=new FileStream(Path.Combine(folder,".lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        switch(command)
        {
            case "train": Train(folder,workers); break;
            case "lock": Lock(folder,seal); break;
            case "assay": Assay(folder,seal); break;
            default: throw new ArgumentException("Unknown seam-cue experiment command.");
        }
    }

    private static ulong GroupKey(string group,int seed)
    {
        ulong hash=ProposalRandom.Key(seed);
        foreach (char c in group) hash=unchecked((hash ^ c)*1099511628211UL);
        return hash;
    }

    // The compact marker changes only the immutable publication read at port 10.
    // The true boundary is the cut between width-1 and 0. A decoy uses one
    // predeclared interior site outside both allowed seam repair neighborhoods.
    internal static RailInput WithCue(CampaignEpisode e,string cue)
    {
        var frame=e.Input;
        if (cue=="original") return frame;
        int edge=Math.Min(frame.TargetGap+1,frame.Width/2);
        int span=frame.Width-2*edge;
        if (span<=0) throw new InvalidDataException("No non-seam decoy site.");
        int decoy=edge+(int)(GroupKey(e.Group,Seed)%(uint)span);
        if (e.Variant==1) decoy=frame.Width-1-decoy;
        return frame with { Seed=cue switch { "seam"=>1UL, "decoy"=>1UL<<decoy, _=>throw new ArgumentException("Unknown cue.") } };
    }

    internal static void CheckOnSyntheticRing()
    {
        var ep=new CampaignEpisode("seam","probe","off-campaign-probe",0,new(13,0,0,0,2,8,2,2,26));
        var original=WithCue(ep,"original");var seam=WithCue(ep,"seam");var decoy=WithCue(ep,"decoy");
        var twin=WithCue(ep with {Variant=1},"decoy");
        void Require(bool value) { if(!value) throw new InvalidDataException("Synthetic seam fixture failed."); }
        Require(original==ep.Input && (seam with {Seed=0})==original && (decoy with {Seed=0})==original);
        Require(seam.Seed==1 && System.Numerics.BitOperations.PopCount(decoy.Seed)==1 &&
            System.Numerics.BitOperations.TrailingZeroCount(decoy.Seed) is >=3 and <=9);
        Require(System.Numerics.BitOperations.TrailingZeroCount(decoy.Seed)+
            System.Numerics.BitOperations.TrailingZeroCount(twin.Seed)==12);
        // One sensor copies port 10; one gated passage moves stock into this site.
        var probe=new OrnamentProgram([new(10,0,10)],[new(2,0,10,0,1)]).Compile();
        var a=probe.Run(seam);var b=probe.Run(decoy);
        Require(a.Ink==seam.Seed && b.Ink==decoy.Seed && a.Edits==1 && b.Edits==1 && a.Stock==1 && b.Stock==1);
        Require(probe.Run(seam,WireMode.Cut).Ink==0 && probe.Run(decoy,WireMode.Cut).Ink==0);
        var idle=TournamentSearch.Idle().Compile();
        Require(idle.Run(seam).Ink==idle.Run(decoy).Ink);
        Console.WriteLine("Synthetic ring passed cue isolation, decoy correspondence, native port 10 wiring, cut and idle controls.");
    }

    private static CampaignEpisode[] Bank(int replicate)
    {
        int seed=unchecked((int)ProposalRandom.Key(Seed,replicate));
        var source=CampaignScience.Episodes("train","seam");
        var strata=CampaignScience.Families.Select(f=>source.Where(e=>e.Family==f).GroupBy(e=>e.Group)
            .OrderBy(g=>GroupKey(g.Key,seed)).Select(g=>g.OrderBy(e=>GroupKey(g.Key+"/"+e.Variant,seed)).First()).ToArray()).ToArray();
        var bank=new CampaignEpisode[TrainCount];
        for(int i=0;i<bank.Length;i++)
        {
            int index=(replicate+i)%strata.Length;
            bank[i]=strata[index][i/strata.Length];
        }
        return bank;
    }
    private static CampaignEpisode[] Apply(CampaignEpisode[] bank,string cue) =>
        bank.Select(e=>e with { Input=WithCue(e,cue) }).ToArray();

    private static Result Evaluate(OrnamentMachine machine,CampaignEpisode episode)
    {
        var physical=machine.Run(episode.Input);
        var score=CampaignScience.Evaluate(episode,physical);
        return new(score.Utility,score.Error,score.Violations,score.Exact,physical.Ink,physical.Edits,physical.Attempts,physical.Blocked,physical.Stock);
    }
    private static TournamentMerit Merit(OrnamentMachine machine,CampaignEpisode[] bank)
    {
        long utility=0, violations=0;int exact=0;
        foreach(var ep in bank)
        {
            var outcome=machine.Run(ep.Input);
            var read=CampaignScience.Evaluate(ep,outcome);
            utility+=read.Utility;violations+=read.Violations;if (read.Exact) exact++;
        }
        return new(utility,violations,exact,bank.Length);
    }
    private static string PoolPath(string folder,int rep)=>Path.Combine(folder,$"pool-{rep}.json.gz");
    private static void Train(string folder,int workers)
    {
        if (File.Exists(Path.Combine(folder,"frozen.json"))) throw new InvalidOperationException("Winner lock already exists.");
        for(int rep=0;rep<Replicates;rep++)
        {
            var bank=Bank(rep);string bankHash=Hash(bank);
            var path=PoolPath(folder,rep);
            if (File.Exists(path)) { ReadPool(folder,rep,bankHash);continue; }
            var arms=Cues.Select(cue=>Apply(bank,cue)).ToArray();
            var records=new Candidate[Proposals];
            Parallel.For(0,Proposals,new ParallelOptions {MaxDegreeOfParallelism=workers},i=>
            {
                var random=new ProposalRandom(ProposalRandom.Key(Seed,rep,i));
                var program=i==0? TournamentSearch.Idle() : TournamentSearch.Random(false,ref random);
                var runtime=program.Compile();
                records[i]=new(i,Convert.ToHexString(program.Encode()), arms.Select(b=>Merit(runtime,b)).ToArray());
            });
            var pool=new Pool(rep,bankHash,Hash(records),records);
            Atomic(path,output=>
            {
                using var zip=new GZipStream(output,CompressionLevel.Fastest,true);
                JsonSerializer.Serialize(zip,pool);
            });
            Console.WriteLine($"Completed replicate {rep+1}/{Replicates}; {Proposals} common proposals evaluated against all three cues.");
        }
    }

    private static Pool ReadPool(string folder,int rep,string bankHash)
    {
        using var stream=File.OpenRead(PoolPath(folder,rep));
        using var zip=new GZipStream(stream,CompressionMode.Decompress);
        var pool=JsonSerializer.Deserialize<Pool>(zip)??throw new InvalidDataException("Missing pool.");
        if (pool.Replicate!=rep || pool.Candidates.Length!=Proposals || pool.TrainingHash!=bankHash || pool.PoolHash!=Hash(pool.Candidates) ||
            pool.Candidates.Where((c,i)=>c.Id!=i || c.Training.Length!=Cues.Length).Any()) throw new InvalidDataException("Pool integrity mismatch.");
        return pool;
    }

    private static IOrderedEnumerable<T> Rank<T>(IEnumerable<T> rows,Func<T,TournamentMerit> score,Func<T,Candidate> source) =>
        rows.OrderByDescending(r=>score(r).Utility).ThenBy(r=>score(r).Violations).ThenByDescending(r=>score(r).Exact)
            .ThenBy(r=>source(r).GenomeHex.Length).ThenBy(r=>source(r).GenomeHex,StringComparer.Ordinal).ThenBy(r=>source(r).Id);
    private static void Lock(string folder,Seal seal)
    {
        string path=Path.Combine(folder,"frozen.json");
        if(File.Exists(path)){ VerifyLock(folder,seal);return; }
        var winners=new List<Winner>();
        var finalists=new List<Finalist>();
        var hashes=new string[Replicates];
        var validation=CampaignScience.Episodes("validation","seam");
        for(int rep=0;rep<Replicates;rep++)
        {
            var pool=ReadPool(folder,rep,Hash(Bank(rep)));
            hashes[rep]=pool.PoolHash;
            for(int arm=0;arm<Cues.Length;arm++)
            {
                var val=Apply(validation,Cues[arm]);
                var ranked=Rank(pool.Candidates,c=>c.Training[arm],c=>c).DistinctBy(c=>c.GenomeHex).Take(Shortlist)
                    .Select(c=>new Finalist(rep,Cues[arm],c,Merit(OrnamentProgram.Decode(Convert.FromHexString(c.GenomeHex)).Compile(),val)));
                var byValidation=Rank(ranked,f=>f.Validation,f=>f.Candidate).ToArray();
                var best=byValidation[0];
                finalists.AddRange(byValidation);
                winners.Add(new(rep,Cues[arm],best.Candidate.Id,best.Candidate.GenomeHex,best.Candidate.Training[arm],best.Validation));
            }
        }
        Save(Path.Combine(folder,"finalists.json"),finalists);
        Save(path,new Locked(Hash(seal),hashes,winners.ToArray(),Hash(winners)));
        Console.WriteLine($"Locked {winners.Count} seam-cue winners before reserve assessment.");
    }

    private static Locked VerifyLock(string folder,Seal seal)
    {
        var locked=Read<Locked>(Path.Combine(folder,"frozen.json"));
        if(locked.SealHash!=Hash(seal) || locked.Winners.Length!=Replicates*Cues.Length || locked.WinnerHash!=Hash(locked.Winners))
            throw new InvalidDataException("Invalid seam-cue winners.");
        for(int rep=0;rep<Replicates;rep++)
        {
            var pool=ReadPool(folder,rep,Hash(Bank(rep)));
            if (pool.PoolHash!=locked.TrainingHashes[rep]) throw new InvalidDataException("Frozen ancestry changed.");
            foreach(var win in locked.Winners.Where(w=>w.Replicate==rep))
                if(pool.Candidates[win.Id].GenomeHex!=win.GenomeHex) throw new InvalidDataException("Frozen program differs from its proposal.");
        }
        return locked;
    }

    private static T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllBytes(path))??throw new InvalidDataException(path);
    private static string Hash<T>(T value)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string HashFile(string path)
    {
        using var stream=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(stream));
    }
    private static void Save<T>(string path,T value)=>Atomic(path,output=>JsonSerializer.Serialize(output,value));
    private static void Atomic(string path,Action<Stream> write)
    {
        string scratch=path+".writing";
        using(var stream=new FileStream(scratch,FileMode.Create,FileAccess.Write,FileShare.None)) write(stream);
        File.Move(scratch,path,true);
    }
    private static void Rows(string path,Action<TextWriter> write)=>Atomic(path,stream=>
    {
        using var text=new StreamWriter(stream,leaveOpen:true);write(text);
    });
    private static void Csv(TextWriter w,params object[] columns)=>w.WriteLine(string.Join(',',columns.Select(c=>
        "\"" + (Convert.ToString(c,CultureInfo.InvariantCulture)??"").Replace("\"","\"\"") + "\"")));
}
