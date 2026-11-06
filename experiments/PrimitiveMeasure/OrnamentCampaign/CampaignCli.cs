using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

public static class CampaignCli
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private sealed record Seal(string Version, DateTimeOffset CreatedUtc, string RuntimeHash,
        TournamentConfig Config, Dictionary<string,string> Manifest, string ProtocolHash);
    public sealed record ReplayInput(string ProgramHex, RailInput Input, WireMode Wire = WireMode.Live);

    public static void Run(string[] args)
    {
        if (args is ["replay", var inputs, var outputs])
        {
            // This path deliberately requires no repository, population, judge,
            // or experiment protocol. Only the local program and published input.
            var jobs = Read<ReplayInput[]>(inputs);
            Write(outputs, jobs.Select(j => OrnamentProgram.Decode(Convert.FromHexString(j.ProgramHex)).Compile()
                .Run(j.Input,j.Wire)).ToArray());
            return;
        }
        if (args.Length is < 2 or > 3)
            throw new ArgumentException("Use freeze/train/lock/assay/fabric/make-replays RUN_FOLDER [workers], or replay INPUT.json OUTPUT.json.");
        var dir = Path.GetFullPath(args[1]);
        var workers = args.Length == 3 ? int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture) :
            Math.Clamp(Environment.ProcessorCount - 2, 1, 8);
        if (workers is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(workers));
        if (args[0] == "freeze") { Freeze(dir); return; }
        var seal = Verify(dir);
        switch (args[0])
        {
            case "train":
                using (var cancellation = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
                    CampaignTournament.Train(dir, seal.Config, seal.ProtocolHash, workers, cancellation.Token);
                }
                break;
            case "lock": CampaignTournament.LockWinners(dir, seal.Config, seal.ProtocolHash); break;
            case "assay": CampaignTournament.Assay(dir, seal.Config, seal.ProtocolHash, workers, "test"); break;
            case "fabric":
                FabricTransfer.Run(Path.Combine(dir,"fabric"), CampaignTournament.ReadFrozen(dir, seal.Config, seal.ProtocolHash));
                break;
            case "make-replays":
                var ranked = CampaignTournament.ReadFrozen(dir, seal.Config, seal.ProtocolHash);
                var jobs = (from w in ranked
                    from family in CampaignScience.Episodes("test", w.Goal).GroupBy(e => (e.Goal, e.Family))
                    from wire in new[] { WireMode.Live, WireMode.Frozen, WireMode.Cut }
                    select new ReplayInput(w.GenomeHex, family.First().Input, wire)).ToArray();
                var outDir = Path.Combine(dir, "replay");
                Directory.CreateDirectory(outDir);
                Write(Path.Combine(outDir, "inputs.json"), jobs);
                Write(Path.Combine(outDir, "expected.json"), jobs.Select(j => OrnamentProgram.Decode(Convert.FromHexString(j.ProgramHex))
                    .Compile().RunReference(j.Input, j.Wire)).ToArray());
                break;
            default: throw new ArgumentException("Unknown campaign command: " + args[0]);
        }
    }

    private static void Freeze(string dir)
    {
        if (Directory.Exists(dir) && Directory.EnumerateFileSystemEntries(dir).Any())
            throw new IOException("Freeze needs a new, empty run directory.");
        var manifest = Path.Combine(dir, "manifest");
        Directory.CreateDirectory(manifest);
        foreach (string document in new[] { "Protocol.md", "ScienceProtocol.md" })
            File.Copy(Path.Combine("experiments","PrimitiveMeasure","OrnamentCampaign",document), Path.Combine(manifest,document));
        CampaignScience.WriteManifest(manifest);
        FabricTransfer.WritePlan(manifest);
        var config = new TournamentConfig();
        var runtimeHash = Hash(typeof(CampaignCli).Assembly.Location);
        var hashes = HashManifest(manifest);
        var protocolHash = Identity(runtimeHash, config, hashes);
        Write(Path.Combine(dir,"protocol.lock.json"), new Seal("ornament-1",DateTimeOffset.UtcNow,runtimeHash,config,hashes,protocolHash));
        Console.WriteLine($"Frozen protocol {protocolHash} with {CampaignScience.Hypotheses.Length} primary hypotheses.");
    }

    private static Seal Verify(string dir)
    {
        var seal = Read<Seal>(Path.Combine(dir,"protocol.lock.json"));
        var actual = HashManifest(Path.Combine(dir,"manifest"));
        if (seal.Version != "ornament-1" || seal.RuntimeHash != Hash(typeof(CampaignCli).Assembly.Location) ||
            actual.Count != seal.Manifest.Count || actual.Any(p => !seal.Manifest.TryGetValue(p.Key,out var hash) || hash != p.Value) ||
            Identity(seal.RuntimeHash,seal.Config,actual) != seal.ProtocolHash)
            throw new InvalidDataException("The frozen executable or protocol differs from this run. Preserve the run and use its frozen files.");
        return seal;
    }

    private static string Identity(string runtimeHash, TournamentConfig config, Dictionary<string,string> files)
    {
        var material = runtimeHash + "\n" + JsonSerializer.Serialize(config) + "\n" +
            string.Join("\n", files.OrderBy(p => p.Key,StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private static Dictionary<string,string> HashManifest(string dir) =>
        Directory.GetFiles(dir,"*",SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .ToDictionary(f => Path.GetRelativePath(dir,f).Replace('\\','/'), Hash);
    internal static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)
        ?? throw new InvalidDataException(path);
    private static void Write<T>(string path, T value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json), Encoding.UTF8);
}
