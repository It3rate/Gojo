using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.OrnamentFollowup;

public sealed record FollowupSpec(string Id, string Prediction, string[] Goals, string PrimaryProtocolHash,
    string PrimaryFrozenSha256, string PrimaryReportSha256, int Seed, int Replicates, int Generations,
    int Population, int TrainingEpisodes, int Shortlist, int Margin)
{
    public string ExpectedRelation { get; init; } = "higher";
}

public static partial class Followup
{
    private static readonly string[] Arms = ["uniform","learned","permuted"];
    private sealed record Binding(string Primary, string SpecHash, string PrimaryReportHash, string RuntimeHash, string PrimaryRuntimeHash);
    private sealed record Gen(int Index, string PreviousHash, TrainingEntry[] Entries);
    private sealed record Ledger(string Hash, Gen Content);
    private sealed record Finalist(TrainingEntry Source, TournamentMerit Validation);
    private sealed record Winner(string Goal, string Arm, int Replicate, int Seed, string TrainingBankHash,
        string HistoryHash, string TicketHash, Finalist[] Finalists);
    private sealed record ChampionLock(string Hash, string SealHash, Winner[] Winners);
    private sealed record GoalMemory(string Goal, MutationEvidence[] Evidence, Dictionary<string,PackedProposer> Arms);
    private const string Protocol = "mutation-family-transfer-1";

    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string Digest<T>(T item) => Hash(JsonSerializer.SerializeToUtf8Bytes(item));
    private static string FileHash(string path)
    {
        using var input = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(input));
    }
    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path))
        ?? throw new InvalidDataException(path);
    private static void Atomic(string path, Action<Stream> write)
    {
        var temporary = path + ".writing";
        using (var stream = File.Create(temporary)) write(stream);
        File.Move(temporary,path,true);
    }
    private static void Write<T>(string path,T item) => Atomic(path,s => JsonSerializer.Serialize(s,item));

    public static void Freeze(string specPath, string primary, string report, string folder)
    {
        primary = Path.GetFullPath(primary);
        folder = Path.GetFullPath(folder);
        if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new IOException("A protocol freeze needs an empty output directory.");
        var spec = Read<FollowupSpec>(specPath);
        Validate(spec);
        var primaryRuntime = FileHash(typeof(CampaignCli).Assembly.Location);
        CheckPrimary(primary,spec,primaryRuntime);
        if (FileHash(report) != spec.PrimaryReportSha256) throw new InvalidDataException("First report differs from the preregistration.");
        Directory.CreateDirectory(folder);
        File.Copy(specPath,Path.Combine(folder,"spec.json"));
        File.Copy(report,Path.Combine(folder,"primary-report.md"));
        var binding = new Binding(primary,FileHash(specPath),spec.PrimaryReportSha256,
            FileHash(typeof(Followup).Assembly.Location),primaryRuntime);
        Write(Path.Combine(folder,"followup-seal.json"), binding);
        Console.WriteLine($"Frozen {spec.Id}: {Digest(binding)}");
    }

    private static void Validate(FollowupSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Id) || string.IsNullOrWhiteSpace(spec.Prediction) ||
            spec.Goals == null || spec.Goals.Length is < 1 or > 2 || spec.Goals.Distinct().Count()!=spec.Goals.Length ||
            spec.Goals.Any(g=>!CampaignTournament.Objectives.Contains(g)) ||
            spec.Replicates is < 1 or > 64 || spec.Generations is < 2 or > 512 ||
            spec.Population is < 8 or > 512 || spec.TrainingEpisodes is < 6 or > 256 ||
            spec.Shortlist is < 1 or > 32 || spec.Margin is < 1 or > 1000 ||
            spec.ExpectedRelation is not ("higher" or "lower" or "equivalent"))
            throw new ArgumentException("Invalid follow-up goals, budget, prediction or decision rule.");
        foreach (var h in new[] { spec.PrimaryProtocolHash,spec.PrimaryFrozenSha256,spec.PrimaryReportSha256 })
            if (h is null || h.Length!=64 || !h.All(Uri.IsHexDigit) || h != h.ToUpperInvariant())
                throw new ArgumentException("Supply uppercase SHA-256 identities from the completed primary study.");
    }

    private static (TournamentConfig Config, FrozenWinner[] Winners) CheckPrimary(string dir, FollowupSpec spec, string runtimeHash)
    {
        using var file = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir,"protocol.lock.json")));
        var root = file.RootElement;
        var config = root.GetProperty("Config").Deserialize<TournamentConfig>()!;
        var manifest = root.GetProperty("Manifest").Deserialize<Dictionary<string,string>>()!;
        string content = runtimeHash + "\n" + JsonSerializer.Serialize(config) + "\n" +
            string.Join("\n", manifest.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>x.Key+"="+x.Value));
        if (root.GetProperty("Version").GetString()!="ornament-1" ||
            root.GetProperty("RuntimeHash").GetString()!=runtimeHash ||
            root.GetProperty("ProtocolHash").GetString()!=spec.PrimaryProtocolHash ||
            Hash(Encoding.UTF8.GetBytes(content))!=spec.PrimaryProtocolHash ||
            FileHash(Path.Combine(dir,"frozen.json"))!=spec.PrimaryFrozenSha256 || spec.Seed==config.Seed)
            throw new InvalidDataException("Primary executable, protocol, winner manifest or fresh seed mismatch.");
        var actualManifest = Directory.GetFiles(Path.Combine(dir,"manifest"),"*",SearchOption.AllDirectories);
        if (actualManifest.Length!=manifest.Count || actualManifest.Any(f=>
                !manifest.TryGetValue(Path.GetRelativePath(Path.Combine(dir,"manifest"),f).Replace('\\','/'),out var h) || h!=FileHash(f)))
            throw new InvalidDataException("Primary manifest differs from its frozen protocol.");
        var winners = CampaignTournament.ReadFrozen(dir,config,spec.PrimaryProtocolHash);
        string reserve = Path.Combine(dir,"reserve");
        if (Directory.Exists(reserve) && Directory.EnumerateFileSystemEntries(reserve).Any())
            throw new InvalidDataException("A primary reserve assay already exists; this cohort cannot be retroactively registered as unseen.");
        string primaryTest = Path.Combine(dir,"test");
        using var test = JsonDocument.Parse(File.ReadAllText(Path.Combine(primaryTest,"complete.json")));
        using var freeze = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir,"frozen.json")));
        var meta = test.RootElement;
        if (meta.GetProperty("Split").GetString()!="test" ||
            meta.GetProperty("LockHash").GetString()!=freeze.RootElement.GetProperty("LockHash").GetString())
            throw new InvalidDataException("The first test assay is incomplete or belongs to another primary lock.");
        var hashes=meta.GetProperty("FileHashes").Deserialize<Dictionary<string,string>>()!;
        foreach (var name in new[]{"cases.csv","champions.csv","hypotheses.csv","groups.csv"})
            if (!hashes.TryGetValue(name,out var hash) || hash!=FileHash(Path.Combine(primaryTest,name)))
                throw new InvalidDataException("The first assay output differs from its completed checksum: "+name);
        return (config,winners);
    }

    public static void Execute(string command, string folder, int workers) =>
        Execute(command,folder,workers,CampaignScience.Episodes);

    internal static void Execute(string command,string folder,int workers,Func<string,string,CampaignEpisode[]> supply)
    {
        if (workers is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(workers));
        using var lease = new FileStream(Path.Combine(folder,".followup.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var seal = Read<Binding>(Path.Combine(folder,"followup-seal.json"));
        var spec = Read<FollowupSpec>(Path.Combine(folder,"spec.json"));
        Validate(spec);
        if (seal.SpecHash!=FileHash(Path.Combine(folder,"spec.json")) ||
            seal.PrimaryReportHash!=FileHash(Path.Combine(folder,"primary-report.md")) ||
            seal.RuntimeHash!=FileHash(typeof(Followup).Assembly.Location) ||
            seal.PrimaryRuntimeHash!=FileHash(typeof(CampaignCli).Assembly.Location))
            throw new InvalidDataException("Follow-up protocol, first report or executable changed.");
        var primary = CheckPrimary(seal.Primary,spec,seal.PrimaryRuntimeHash);
        var oldSeeds=primary.Winners.Select(w=>w.Seed).ToHashSet();
        if (Enumerable.Range(0,spec.Replicates).Any(r=>oldSeeds.Contains(FreshSeed(spec,r))))
            throw new InvalidDataException("A follow-up random stream duplicates a primary search seed.");
        var sealHash = Digest(seal);
        if (command == "train")
        {
            if (File.Exists(Path.Combine(folder,"frozen.json"))) { ReadLock(folder,sealHash,spec); return; }
            var memories = spec.Goals.Select(goal => Learn(goal,seal.Primary,spec.PrimaryProtocolHash,primary.Config,primary.Winners)).ToArray();
            Write(Path.Combine(folder,"proposer.json"),memories);
            CsvFile(Path.Combine(folder,"mutation-evidence.csv"), output =>
            {
                output.WriteLine("goal,edit_family,all_paid_single_parent_attempts,strict_utility_improvements,tickets");
                foreach (var m in memories) foreach (var e in m.Evidence)
                    Row(output,m.Goal,e.Family,e.Attempts,e.Improved,e.Tickets);
            });
            var jobs = from m in memories from arm in Arms from rep in Enumerable.Range(0,spec.Replicates) select (m,arm,rep);
            Parallel.ForEach(jobs,new ParallelOptions{MaxDegreeOfParallelism=workers}, j=>
            {
                TrainIsland(folder,spec,j.m,j.arm,j.rep,supply);
                Console.WriteLine($"Completed {j.m.Goal}/{j.arm}/{j.rep}.");
            });
        }
        else if (command == "lock")
        {
            var memorized = Read<GoalMemory[]>(Path.Combine(folder,"proposer.json"));
            var expected = spec.Goals.Select(goal=>Learn(goal,seal.Primary,spec.PrimaryProtocolHash,primary.Config,primary.Winners)).ToArray();
            if (Digest(memorized)!=Digest(expected)) throw new InvalidDataException("Proposal distributions differ from the verified learning histories.");
            Lock(folder,spec,sealHash,supply);
        }
        else if (command == "assay") Assay(folder,spec,ReadLock(folder,sealHash,spec),supply);
        else throw new ArgumentException("Choose train, lock or assay.");
    }

    // This is the primary train/lock schema, including the case of each JSON
    // property name. The first generation is already linked to the exact
    // protocol, island, seed and episode banks, not an unbound empty prefix.
    private static string PrimaryGenesis(string protocolHash,TournamentConfig config,FrozenWinner source) =>
        Digest(new{protocolHash, config, goal=source.Goal, method=source.Method, replicate=source.Replicate,
            seed=source.Seed, train=source.TrainingBankHash, validation=source.ValidationBankHash});

    public static void VerifySource(string specPath,string primary)
    {
        var spec = Read<FollowupSpec>(specPath);
        Validate(spec);
        var source = CheckPrimary(primary,spec,FileHash(typeof(CampaignCli).Assembly.Location));
        foreach(var goal in spec.Goals)
        foreach(var winner in source.Winners.Where(w=>w.Goal==goal && w.Method=="evolve"))
        {
            // Only integrity is reported. This command never calculates
            // family weights, executes a candidate or opens any reserve data.
            foreach(var _ in PrimaryLedgers(primary,spec.PrimaryProtocolHash,source.Config,winner)) { }
            Console.WriteLine($"{goal}/evolve/{winner.Replicate:D2}: verified {source.Config.Generations} generations, {winner.HistoryHash}");
        }
    }

    private static IEnumerable<Ledger> PrimaryLedgers(string primary,string protocolHash,TournamentConfig config,FrozenWinner source)
    {
        var dir = Path.Combine(primary,"islands",source.Goal,"evolve",source.Replicate.ToString("D2",CultureInfo.InvariantCulture));
        string previous = PrimaryGenesis(protocolHash,config,source);
        for(int g=0;g<config.Generations;g++)
        {
            var ledger=LoadLedger(Path.Combine(dir,$"g{g:D4}.json.gz"),g,previous,config.Population);
            previous=ledger.Hash;
            yield return ledger;
        }
        if(previous!=source.HistoryHash) throw new InvalidDataException("Evolution source differs from its primary locked chain.");
    }

    private static GoalMemory Learn(string goal, string primary, string protocolHash,TournamentConfig config, FrozenWinner[] frozen)
    {
        var memory = new ProposalMemory();
        var sources = frozen.Where(w=>w.Goal==goal && w.Method=="evolve").OrderBy(w=>w.Replicate).ToArray();
        if (!sources.Select(s=>s.Replicate).SequenceEqual(Enumerable.Range(0,config.Replicates)))
            throw new InvalidDataException("Missing or duplicate source evolution replicate.");
        foreach (var source in sources)
        {
            var history = new Dictionary<int,long>();
            foreach(var ledger in PrimaryLedgers(primary,protocolHash,config,source))
            {
                int g=ledger.Content.Index;
                foreach (var row in ledger.Content.Entries)
                {
                    int family = Array.IndexOf(ProposalMemory.Families,row.Edit);
                    if (family >= 0)
                    {
                        if (row.ParentA is not int parent || row.ParentB != null || !history.TryGetValue(parent,out var utility) ||
                            parent >= g*config.Population) throw new InvalidDataException("Mutation lacks its prior generation parent.");
                        memory.Observe(family,utility,row.Training.Utility);
                    }
                    else if (row.ParentA != null && row.ParentB == null)
                        throw new InvalidDataException("Unrecognized single-parent edit family.");
                    history.Add(row.Id,row.Training.Utility);
                }
            }
        }
        var evidence = memory.Evidence();
        return new(goal,evidence,Arms.ToDictionary(a=>a,a=>ProposalMemory.Pack(evidence,a)));
    }

    private static Ledger LoadLedger(string path, int index, string previous, int population)
    {
        using var stream = File.OpenRead(path);
        using var zip = new GZipStream(stream,CompressionMode.Decompress);
        var ledger = JsonSerializer.Deserialize<Ledger>(zip) ?? throw new InvalidDataException(path);
        if (ledger.Content.Index!=index || ledger.Content.PreviousHash!=previous ||
            ledger.Content.Entries.Length!=population || ledger.Hash!=Digest(ledger.Content) ||
            ledger.Content.Entries.Where((e,p)=>e.Id!=index*population+p).Any())
            throw new InvalidDataException("Generation checksum, ancestry or row index mismatch: "+path);
        return ledger;
    }

    private static void CsvFile(string path, Action<TextWriter> write) => Atomic(path, stream =>
    {
        using var writer = new StreamWriter(stream, leaveOpen:true);
        write(writer);
    });
    private static void Row(TextWriter writer, params object?[] fields) =>
        writer.WriteLine(string.Join(',',fields.Select(f=>"\""+(Convert.ToString(f,CultureInfo.InvariantCulture)??"").Replace("\"","\"\"")+"\"")));
}
