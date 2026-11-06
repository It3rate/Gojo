using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

public sealed record TournamentConfig(int Seed = 620261106, int Generations = 64, int Population = 64,
    int Replicates = 4, int TrainingEpisodes = 32, int Shortlist = 8);

public sealed record TournamentMerit(long Utility, long Violations, int Exact, int Episodes);
public sealed record TrainingEntry(int Id, string GenomeHex, TournamentMerit Training, string Behavior,
    int? ParentA, int? ParentB, string Edit);
public sealed record FrozenWinner(string Goal, string Method, int Seed, int Replicate, int Id, string GenomeHex,
    TournamentMerit Training, TournamentMerit Validation, int? ParentA, int? ParentB, string Edit,
    string TrainingBankHash, string ValidationBankHash, string HistoryHash);

public static partial class CampaignTournament
{
    public static readonly string[] Objectives = ["balance", "infill", "mirror", "seam", "mixed"];
    public static readonly string[] Methods = ["parameter", "open", "random", "evolve"];
    public static int DefaultWorkers => Math.Clamp(Environment.ProcessorCount - 2, 1, 8);
    private const string Format = "ornament-league-1";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = false };
    private sealed record Parameters(string Format, string ProtocolHash, TournamentConfig Config);
    private sealed record GenerationContent(int Index, string PreviousHash, TrainingEntry[] Entries);
    private sealed record GenerationFile(string Hash, GenerationContent Content);
    private sealed record ValidationRow(TrainingEntry Source, TournamentMerit Validation);
    private sealed record IslandResult(FrozenWinner Winner, ValidationRow[] Finalists, TrainingEntry Worst,
        int Charged, int DistinctGenomes, int DistinctBehaviors);
    private sealed record FreezeContent(string ConfigHash, string ProtocolHash, FrozenWinner[] Winners);
    private sealed record FreezeFile(string LockHash, FreezeContent Content);

    private sealed class Island(string directory, string goal, string method, int replicate, int seed,
        CampaignEpisode[] train, CampaignEpisode[] validation, int shortlistSize, string genesis)
    {
        public readonly string Directory = directory, Goal = goal, Method = method;
        public readonly int Replicate = replicate, Seed = seed;
        public readonly CampaignEpisode[] Train = train, Validation = validation;
        public readonly string BankHash = BankDigest(train), ValidationHash = BankDigest(validation);
        public readonly List<TrainingEntry> Shortlist = [];
        public readonly HashSet<string> Genomes = [], Behaviors = [];
        public readonly TrainingEntry[][] Parents = new TrainingEntry[TournamentSearch.Demes][];
        public int NextGeneration { get; private set; }
        public string LastHash { get; private set; } = genesis;
        public TrainingEntry? Worst { get; private set; }

        public void Absorb(GenerationFile generation)
        {
            var rows = generation.Content.Entries;
            foreach (var row in rows)
            {
                Genomes.Add(row.GenomeHex);
                Behaviors.Add(row.Behavior);
                if (Worst == null || Compare(row, Worst) > 0) Worst = row;
            }
            // Distinct source programs only. Selection has no validation or test access.
            var finalists = Shortlist.Concat(rows).Order(EntryOrder)
                .DistinctBy(row => row.GenomeHex).Take(shortlistSize).ToArray();
            Shortlist.Clear();
            Shortlist.AddRange(finalists);
            for (int d = 0; d < Parents.Length; d++)
            {
                var local = rows.Where(row => row.Id % Parents.Length == d).Order(EntryOrder).ToArray();
                // Two highest-ranked individuals and two distinct observed behaviors.
                Parents[d] = local.Take(2).Concat(local.DistinctBy(x => x.Behavior).Take(4)).DistinctBy(x => x.Id).Take(4).ToArray();
            }
            NextGeneration++;
            LastHash = generation.Hash;
        }
    }

    private static readonly IComparer<TrainingEntry> EntryOrder = Comparer<TrainingEntry>.Create(Compare);

    private static int CompareMerit(TournamentMerit a, TournamentMerit b)
    {
        int result = b.Utility.CompareTo(a.Utility); // Same fixed episode bank within each comparison.
        if (result == 0) result = a.Violations.CompareTo(b.Violations);
        if (result == 0) result = b.Exact.CompareTo(a.Exact);
        return result;
    }

    private static int Compare(TrainingEntry a, TrainingEntry b)
    {
        int result = CompareMerit(a.Training, b.Training);
        if (result == 0) result = a.GenomeHex.Length.CompareTo(b.GenomeHex.Length);
        if (result == 0) result = string.CompareOrdinal(a.GenomeHex, b.GenomeHex);
        return result != 0 ? result : a.Id.CompareTo(b.Id);
    }

    public static void Train(string folder, TournamentConfig config, string protocolHash, int workers,
        CancellationToken cancel = default, Func<string,string,CampaignEpisode[]>? readBank = null)
    {
        Check(config, protocolHash, workers);
        Directory.CreateDirectory(folder);
        using var lease = Lease(folder);
        Bind(folder, config, protocolHash, create: true);
        if (File.Exists(Path.Combine(folder, "frozen.json")))
        {
            ReadFrozen(folder, config, protocolHash);
            return;
        }
        var islands = new List<Island>();
        foreach (var goal in Objectives)
        {
            var corpus = readBank ?? CampaignScience.Episodes;
            var all = corpus("train", goal);
            var validation = corpus("validation", goal);
            if (validation.Length == 0) throw new ArgumentException("A validation bank is required.");
            for (int replicate = 0; replicate < config.Replicates; replicate++)
            {
                int seed = ReplicateSeed(config.Seed, replicate);
                var bank = Balanced(all, config.TrainingEpisodes, seed);
                foreach (var method in Methods)
                {
                    var genesis = Digest(new { protocolHash, config, goal, method, replicate, seed,
                        train = BankDigest(bank), validation = BankDigest(validation) });
                    var island = new Island(IslandDirectory(folder, goal, method, replicate), goal, method,
                        replicate, seed, bank, validation, config.Shortlist, genesis);
                    Directory.CreateDirectory(island.Directory);
                    Restore(island, config);
                    islands.Add(island);
                }
            }
        }
        var options = new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = cancel };
        // All independent tournaments meet the same generation barrier; embryos
        // and parents never depend on thread completion order or wall time.
        for (int gen = 0; gen < config.Generations; gen++)
        {
            int index = gen;
            Parallel.ForEach(islands.Where(island => island.NextGeneration == index), options,
                island => RunGeneration(island, config, index));
            SaveJson(Path.Combine(folder, "progress.json"), new { CompletedGenerations = gen + 1,
                ConfiguredGenerations = config.Generations, Islands = islands.Count,
                Charged = (gen + 1) * (long)islands.Count * config.Population,
                TrainingEpisodes = (gen + 1) * (long)islands.Count * config.Population * config.TrainingEpisodes });
        }
        Parallel.ForEach(islands, options, island => WriteWinner(island, config));
    }

    private static int ReplicateSeed(int seed, int replicate) =>
        (int)(ProposalRandom.Key(seed, replicate) & 0x7FFFFFFF);

    // Fixed, disjoint recipe groups are selected round-robin across goal/family
    // strata. Prefer a second group before a sibling variant of an earlier group.
    private static CampaignEpisode[] Balanced(CampaignEpisode[] source, int n, int seed)
    {
        if (source.Length < n) throw new InvalidOperationException("Insufficient distinct training episodes.");
        var strata = source.GroupBy(e => (e.Goal, e.Family)).OrderBy(g => g.Key.Goal, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Family, StringComparer.Ordinal).Select(g =>
            {
                var groups = g.GroupBy(e => e.Group).OrderBy(k => HashString(seed, k.Key)).ToArray();
                var ordered = groups.Select(h => h.OrderBy(e => HashString(seed, e.Group + "/" + e.Variant)).ToArray()).ToArray();
                var interleaved = new Queue<CampaignEpisode>();
                for (int v = 0; v < ordered.Max(h => h.Length); v++)
                    foreach (var group in ordered)
                        if (v < group.Length) interleaved.Enqueue(group[v]);
                return interleaved;
            }).ToArray();
        var result = new List<CampaignEpisode>(n);
        int offset = seed % strata.Length;
        for (int visit = 0; result.Count < n; visit++)
        {
            var queue = strata[(offset + visit) % strata.Length];
            if (queue.TryDequeue(out var episode)) result.Add(episode);
        }
        return result.ToArray();
    }

    private static ulong HashString(int seed, string value)
    {
        ulong hash = ProposalRandom.Key(seed);
        foreach (char c in value) hash = unchecked((hash ^ c) * 1099511628211UL);
        return hash;
    }

    private static string BankDigest(CampaignEpisode[] bank) =>
        Digest(bank.Select(e => new { e.Goal, e.Family, e.Group, e.Variant, e.Input }).ToArray());
    private static string IslandDirectory(string folder, string goal, string method, int replicate) =>
        Path.Combine(folder, "islands", goal, method, replicate.ToString("D2", CultureInfo.InvariantCulture));
    private static string GenerationPath(Island i, int index) =>
        Path.Combine(i.Directory, $"g{index:D4}.json.gz");

    private static void Restore(Island island, TournamentConfig config)
    {
        for (int index = 0; index < config.Generations; index++)
        {
            var file = GenerationPath(island, index);
            if (!File.Exists(file)) break;
            using var input = File.OpenRead(file);
            using var zip = new GZipStream(input, CompressionMode.Decompress);
            var record = JsonSerializer.Deserialize<GenerationFile>(zip, Json)
                ?? throw new InvalidDataException(file);
            if (record.Content.Index != index || record.Content.Entries.Length != config.Population ||
                record.Content.PreviousHash != island.LastHash || record.Hash != Digest(record.Content) ||
                record.Content.Entries.Where((e, pos) => e.Id != index * config.Population + pos).Any())
                throw new InvalidDataException("Generation ledger mismatch: " + file);
            island.Absorb(record);
        }
        if (Directory.EnumerateFiles(island.Directory, "g*.json.gz").Count() != island.NextGeneration)
            throw new InvalidDataException("Non-contiguous generation ledger in " + island.Directory);
    }

    private static void RunGeneration(Island island, TournamentConfig config, int gen)
    {
        var rows = new TrainingEntry[config.Population];
        int objective = Array.IndexOf(Objectives, island.Goal);
        // Evolve and random receive identical first-generation programs in each replicate.
        int stream = island.Method == "evolve" && gen == 0 ? Array.IndexOf(Methods, "random") : Array.IndexOf(Methods, island.Method);
        for (int pos = 0; pos < rows.Length; pos++)
        {
            int id = checked(gen * config.Population + pos);
            var rng = new ProposalRandom(ProposalRandom.Key(island.Seed, objective, stream, gen, pos));
            var child = TournamentSearch.Create(island.Method, id, gen, pos, island.Seed, island.Parents, ref rng);
            var fitness = Evaluate(child.Program.Compile(), island.Train, out var fingerprint);
            rows[pos] = new(id, Convert.ToHexString(child.Program.Encode()), fitness, fingerprint,
                child.ParentA, child.ParentB, child.Edit);
        }
        var content = new GenerationContent(gen, island.LastHash, rows);
        var result = new GenerationFile(Digest(content), content);
        var path = GenerationPath(island, gen);
        Atomic(path, stream =>
        {
            using var compression = new GZipStream(stream, CompressionLevel.Fastest, leaveOpen:true);
            JsonSerializer.Serialize(compression, result, Json);
        });
        island.Absorb(result);
    }

    private static TournamentMerit Evaluate(OrnamentMachine machine, CampaignEpisode[] bank, out string fingerprint)
    {
        long utility = 0, violations = 0;
        int exact = 0;
        ulong behavior = 14695981039346656037UL;
        foreach (var episode in bank)
        {
            var result = machine.Run(episode.Input);
            var score = CampaignScience.Evaluate(episode, result);
            utility += score.Utility;
            violations += score.Violations;
            if (score.Exact) exact++;
            behavior = unchecked((behavior ^ result.Ink) * 1099511628211UL);
            behavior = unchecked((behavior ^ (uint)result.Edits) * 1099511628211UL);
        }
        fingerprint = behavior.ToString("X16", CultureInfo.InvariantCulture);
        return new(utility, violations, exact, bank.Length);
    }

    private static void WriteWinner(Island island, TournamentConfig config)
    {
        var finalists = island.Shortlist.Select(entry => new ValidationRow(entry,
            Evaluate(OrnamentProgram.Decode(Convert.FromHexString(entry.GenomeHex)).Compile(), island.Validation, out _)))
            .OrderBy(row => row.Validation, Comparer<TournamentMerit>.Create(CompareMerit))
            .ThenBy(row => row.Source, EntryOrder).ToArray();
        var best = finalists[0];
        var source = best.Source;
        var winner = new FrozenWinner(island.Goal, island.Method, island.Seed, island.Replicate,
            source.Id, source.GenomeHex, source.Training, best.Validation, source.ParentA, source.ParentB,
            source.Edit, island.BankHash, island.ValidationHash, island.LastHash);
        SaveJson(Path.Combine(island.Directory, "winner.json"), new IslandResult(winner, finalists,
            island.Worst!, checked(config.Generations * config.Population), island.Genomes.Count, island.Behaviors.Count));
    }

    public static FrozenWinner[] LockWinners(string folder, TournamentConfig config, string protocolHash)
    {
        using var lease = Lease(folder);
        Bind(folder, config, protocolHash, create:false);
        var path = Path.Combine(folder, "frozen.json");
        if (File.Exists(path)) return ReadFrozen(folder, config, protocolHash);
        var winners = new List<FrozenWinner>();
        foreach (var goal in Objectives)
        foreach (var method in Methods)
        for (int rep = 0; rep < config.Replicates; rep++)
        {
            string directory = IslandDirectory(folder, goal, method, rep);
            var result = ReadJson<IslandResult>(Path.Combine(directory, "winner.json"));
            var w = result.Winner;
            if (w.Goal != goal || w.Method != method || w.Replicate != rep || w.Seed != ReplicateSeed(config.Seed, rep) ||
                result.Charged != checked(config.Generations * config.Population) || result.Finalists.Length == 0 ||
                !MatchesWinner(result.Finalists[0], w))
                throw new InvalidDataException("Incomplete island: " + directory);
            // Verify the entire on-disk ancestry and the winning genotype
            // before crossing from training/validation into blind testing.
            string history = Digest(new { protocolHash, config, goal, method, replicate=rep, seed=w.Seed,
                train=w.TrainingBankHash, validation=w.ValidationBankHash });
            bool present = false;
            for (int gen=0; gen<config.Generations; gen++)
            {
                using var stream = File.OpenRead(Path.Combine(directory,$"g{gen:D4}.json.gz"));
                using var gzip = new GZipStream(stream,CompressionMode.Decompress);
                var ledger = JsonSerializer.Deserialize<GenerationFile>(gzip, Json) ?? throw new InvalidDataException(directory);
                if (ledger.Content.Index != gen || ledger.Content.PreviousHash != history ||
                    ledger.Hash != Digest(ledger.Content) || ledger.Content.Entries.Length != config.Population)
                    throw new InvalidDataException("Frozen ancestry mismatch: " + directory);
                history=ledger.Hash;
                if (gen == w.Id / config.Population)
                    present = ledger.Content.Entries[w.Id % config.Population] == result.Finalists[0].Source;
            }
            if (!present || history != w.HistoryHash) throw new InvalidDataException("Winner differs from ancestry: " + directory);
            winners.Add(w);
        }
        var content = new FreezeContent(ConfigHash(config, protocolHash), protocolHash, winners.ToArray());
        SaveJson(path, new FreezeFile(Digest(content), content));
        return content.Winners;
    }

    private static bool MatchesWinner(ValidationRow best,FrozenWinner winner) =>
        best.Source.Id == winner.Id && best.Source.GenomeHex == winner.GenomeHex &&
        best.Source.Training == winner.Training && best.Validation == winner.Validation &&
        best.Source.ParentA == winner.ParentA && best.Source.ParentB == winner.ParentB && best.Source.Edit == winner.Edit;

    public static FrozenWinner[] ReadFrozen(string folder, TournamentConfig config, string protocolHash)
    {
        Bind(folder, config, protocolHash, create:false);
        var frozen = ReadJson<FreezeFile>(Path.Combine(folder, "frozen.json"));
        if (frozen.LockHash != Digest(frozen.Content) || frozen.Content.ConfigHash != ConfigHash(config, protocolHash) ||
            frozen.Content.ProtocolHash != protocolHash || frozen.Content.Winners.Length != Objectives.Length * Methods.Length * config.Replicates)
            throw new InvalidDataException("Invalid frozen winner manifest.");
        foreach (var winner in frozen.Content.Winners)
            _ = OrnamentProgram.Decode(Convert.FromHexString(winner.GenomeHex));
        return frozen.Content.Winners;
    }

    private static string ConfigHash(TournamentConfig config, string protocolHash) =>
        Digest(new Parameters(Format, protocolHash, config));
    private static void Bind(string folder, TournamentConfig config, string protocolHash, bool create)
    {
        var expected = new Parameters(Format, protocolHash, config);
        string path = Path.Combine(folder, "parameters.json");
        if (File.Exists(path))
        {
            if (ReadJson<Parameters>(path) != expected)
                throw new InvalidOperationException("This folder belongs to a different protocol or budget.");
        }
        else if (create) SaveJson(path, expected);
        else throw new InvalidDataException("Missing tournament parameters.");
    }

    private static void Check(TournamentConfig c, string hash, int workers)
    {
        if (c.Generations < 1 || c.Population < TournamentSearch.Demes || c.Population % TournamentSearch.Demes != 0 ||
            c.Replicates < 1 || c.TrainingEpisodes < 1 || c.Shortlist < 1 || workers < 1 || string.IsNullOrWhiteSpace(hash))
            throw new ArgumentOutOfRangeException(nameof(c));
    }

    private static FileStream Lease(string folder) => new(Path.Combine(folder, ".training.lock"),
        FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    private static T ReadJson<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), Json)
        ?? throw new InvalidDataException("Missing JSON object: " + path);
    private static string Digest<T>(T content) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(content, Json)));
    private static void SaveJson<T>(string path, T content) =>
        Atomic(path, output => JsonSerializer.Serialize(output, content, Json));
    private static void Atomic(string path, Action<Stream> write)
    {
        string scratch = path + ".writing";
        using (var output = new FileStream(scratch, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            write(output);
            output.Flush();
        }
        File.Move(scratch, path, overwrite:true);
    }
}
