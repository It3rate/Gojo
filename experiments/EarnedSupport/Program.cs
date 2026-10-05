using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using MarketTraining;
using M = NegotiatedPorts.MarketProbe;
using S = EarnedSupport.SupportNetwork;

namespace EarnedSupport;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions Compact = new();
    private sealed record Manifest(string Format, string Scenario, long Seed, long GainNumerator,
        long GainDenominator, int Window, int Checkpoint, string SourceFingerprint,
        string ProtocolFingerprint, DateTimeOffset CreatedUtc);
    private sealed record Continuation(string ManifestHash, ExperimentSource Environment,
        long WindowBytes, WindowTotals Totals, long Checkpoints);

    public static int Main(string[] args)
    {
        try
        {
            if (args.SequenceEqual(new[] { "--checks-only" }))
            {
                Console.WriteLine($"{Checks.Run()} earned-support check groups passed.");
                return 0;
            }
            var options = Options.Parse(args);
            if (options.Campaign)
            {
                if (Directory.Exists(options.Directory) && Directory.EnumerateFileSystemEntries(options.Directory).Any())
                    throw new InvalidOperationException("Campaign output must be a new empty directory.");
                foreach (var seed in new long[] { 11, 23, 47 })
                foreach (var (numerator, denominator) in new (long, long)[] { (0, 1), (1, 2), (1, 1), (2, 1) })
                    if (!Run(options with { Campaign = false, Directory = Path.Combine(options.Directory,
                            $"periodic-{seed}-{numerator}-{denominator}"), Seed = seed, Scenario = "periodic",
                            Numerator = numerator, Denominator = denominator, Steps = 1440 })) return 0;
                foreach (var scenario in new[] { "flat-return", "random-sign" })
                foreach (var gain in new long[] { 0, 1 })
                    if (!Run(options with { Campaign = false, Directory = Path.Combine(options.Directory,
                            $"{scenario}-11-{gain}-1"), Seed = 11, Scenario = scenario,
                            Numerator = gain, Denominator = 1, Steps = 1440 })) return 0;
            }
            else Run(options);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }

    private static bool Run(Options options)
    {
        if (!options.Resume && Directory.Exists(options.Directory) && Directory.EnumerateFileSystemEntries(options.Directory).Any())
            throw new InvalidOperationException("Output is not empty; choose a new directory or --resume.");
        Directory.CreateDirectory(options.Directory);
        using var writerLock = new FileStream(Path.Combine(options.Directory, ".writer.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var repository = Repository();
        var sourceHash = Fingerprint(repository);
        var protocolHash = Hash(File.ReadAllBytes(Path.Combine(repository, "experiments/EarnedSupport/PROTOCOL.md")));
        var manifestPath = Path.Combine(options.Directory, "manifest.json");
        Manifest manifest;
        Relation root;
        ExperimentSource environment;
        WindowTotals totals;
        long offset = 0, checkpoints = 0;
        string manifestHash;
        if (options.Resume)
        {
            var bytes = File.ReadAllBytes(manifestPath);
            manifestHash = Hash(bytes);
            manifest = JsonSerializer.Deserialize<Manifest>(bytes, Json)!;
            if (manifest.Format != "earned-support-v1" || manifest.SourceFingerprint != sourceHash || manifest.ProtocolFingerprint != protocolHash)
                throw new InvalidDataException("Resume requires the same source, build and frozen protocol.");
            using var archive = ZipFile.OpenRead(Path.Combine(options.Directory, "latest.checkpoint.zip"));
            using var stateStream = archive.GetEntry("runner.json")!.Open();
            var state = JsonSerializer.Deserialize<Continuation>(stateStream, Json)!;
            if (state.ManifestHash != manifestHash) throw new InvalidDataException("Checkpoint and manifest differ.");
            using var nativeStream = archive.GetEntry("native.gojo")!.Open();
            using var buffer = new MemoryStream(); nativeStream.CopyTo(buffer);
            var roots = GojoNumericFormat.Decode(buffer.ToArray());
            if (roots is not [Relation restored]) throw new InvalidDataException("Missing native world.");
            root = restored;
            environment = state.Environment; totals = state.Totals; offset = state.WindowBytes; checkpoints = state.Checkpoints;
            if (M.Version(S.Market(root)) != environment.Source.Position ||
                ((CarrierLocation)M.Parts(S.Market(root))[3]).Ticks != environment.Source.Position ||
                S.SignedValue(S.LastSettledVersion(root)).Ticks != environment.Source.Position ||
                environment.Scenario != manifest.Scenario || environment.Source.Seed != manifest.Seed ||
                !M.Pass(S.Conservation(root)))
                throw new InvalidDataException("Restored clock or supply equation does not agree.");
        }
        else
        {
            manifest = new("earned-support-v1", options.Scenario, options.Seed, options.Numerator, options.Denominator,
                options.Window, options.Checkpoint, sourceHash, protocolHash, DateTimeOffset.UtcNow);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, Json);
            File.WriteAllBytes(manifestPath, bytes); manifestHash = Hash(bytes);
            root = S.ReleaseFrame(S.Create(options.Numerator, options.Denominator));
            environment = new() { Scenario = options.Scenario, Source = new() { Seed = options.Seed } };
            totals = new();
        }
        using var windows = new FileStream(Path.Combine(options.Directory, "windows.jsonl"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        if (windows.Length < offset) throw new InvalidDataException("Report is shorter than committed checkpoint; refusing to pad lost evidence.");
        windows.SetLength(offset); windows.Position = offset;
        var window = new WindowTotals();
        var stop = false;
        ConsoleCancelEventHandler cancel = (_, args) => { args.Cancel = true; stop = true; };
        Console.CancelKeyPress += cancel;
        try
        {
            for (var step = 0; step < options.Steps && !stop; step++)
            {
                var sample = environment.Next();
                var allocated = GC.GetTotalAllocatedBytes(true);
                var started = Stopwatch.GetTimestamp();
                if (!S.TryObserve(root, sample.Input, out var next, out var frame, out _))
                    throw new InvalidDataException("Native admission/earned settlement was unresolved or stale.");
                root = next;
                var nativeTime = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var nativeAllocation = GC.GetTotalAllocatedBytes(true) - allocated;
                allocated = GC.GetTotalAllocatedBytes(true); started = Stopwatch.GetTimestamp();
                Observation.Add(window, S.Market(root), frame, sample.Phase);
                Observation.Add(totals, S.Market(root), frame, sample.Phase);
                if (!M.Pass(S.Conservation(root))) throw new InvalidDataException("Initial-plus-issued resource accounting failed.");
                root = S.ReleaseFrame(root);
                var observationTime = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var observationAllocation = GC.GetTotalAllocatedBytes(true) - allocated;
                foreach (var aggregate in new[] { window, totals })
                {
                    aggregate.NativeWallMilliseconds += nativeTime; aggregate.NativeAllocationBytes += nativeAllocation;
                    aggregate.ObserverWallMilliseconds += observationTime; aggregate.ObserverAllocationBytes += observationAllocation;
                }
                if (!window.Conserved) throw new InvalidDataException("The inner market supply claim failed.");
                if (environment.Source.Position % manifest.Window == 0) Flush("window");
                if (environment.Source.Position % manifest.Checkpoint == 0) { Flush("checkpoint"); Save(); }
            }
            Flush(stop ? "interrupted" : "requested-end"); Save();
            File.WriteAllText(Path.Combine(options.Directory, "summary.json"), JsonSerializer.Serialize(new
            {
                ManifestHash = manifestHash, Reason = stop ? "interrupted" : "requested-end",
                environment.Source.Position, Totals = totals, Current = State(root), Checkpoints = checkpoints,
                Limit = "Supplied benefit/cost calibration and fixed offer population; added currency is not additional capacity or proof of regulation."
            }, Json));
            Console.WriteLine($"{Path.GetFileName(options.Directory)}: {environment.Source.Position} arrivals, issued {S.Issued(root).Value}, " +
                $"reservoir {M.Reservoir(S.Market(root)).Value}, spent {M.Spent(S.Market(root)).Value}.");
        }
        finally { Console.CancelKeyPress -= cancel; }
        return !stop;

        void Flush(string reason)
        {
            if (window.Admissions == 0) return;
            var row = JsonSerializer.SerializeToUtf8Bytes(new { Reason = reason, Window = window, Current = State(root) }, Compact);
            windows.Write(row); windows.WriteByte((byte)'\n'); windows.Flush(true); window = new();
        }
        void Save()
        {
            checkpoints++;
            var started = Stopwatch.GetTimestamp(); var allocated = GC.GetTotalAllocatedBytes(true);
            var path = Path.Combine(options.Directory, "latest.checkpoint.zip");
            using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    using (var entry = archive.CreateEntry("native.gojo", CompressionLevel.Fastest).Open())
                        entry.Write(GojoNumericFormat.Encode(root));
                    using (var entry = archive.CreateEntry("runner.json", CompressionLevel.Fastest).Open())
                        JsonSerializer.Serialize(entry, new Continuation(manifestHash, environment, windows.Position, totals, checkpoints), Json);
                }
                stream.Flush(true);
            }
            File.Move(path + ".tmp", path, true);
            var graph = GojoEncodedSnapshot.CaptureGraph(root);
            File.WriteAllText(Path.Combine(options.Directory, "latest-checkpoint-inspection.json"), JsonSerializer.Serialize(new
            {
                environment.Source.Position, Carriers = graph.Carriers.Length, Relations = graph.Relations.Length,
                Bytes = new FileInfo(path).Length, WallMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                AllocationBytes = GC.GetTotalAllocatedBytes(true) - allocated, State = State(root)
            }, Json));
        }
    }

    private static object State(Relation root) => new
    {
        Market = Observation.State(S.Market(root)), Issued = S.Issued(root).Value,
        Traces = S.States(root).Select(s => new
        {
            Value = S.Trace(s).Value, Tension = S.Trace(s).Tension,
            DivisionRemainder = S.SignedValue(S.TraceRemainder(s)).Ticks,
            PaymentRemainder = S.SignedValue(S.PayoutRemainder(s)).Ticks
        }).ToArray(),
        LastSettledVersion = S.SignedValue(S.LastSettledVersion(root)).Ticks,
        Accounted = M.Pass(S.Conservation(root))
    };
    private static string Repository()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Gojo.slnx"))) return directory.FullName;
                directory = directory.Parent;
            }
        }
        throw new InvalidOperationException("Run from a Gojo checkout or its build directory.");
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string Fingerprint(string repository)
    {
        var sources = new[] { "experiments/NegotiatedPorts/MarketProbe.cs", "experiments/ConnectionAssembly/NativeMath.cs",
                "experiments/MarketTraining/Observation.cs", "experiments/MarketTraining/EnvironmentSource.cs" }
            .Select(p => Path.Combine(repository, p))
            .Concat(Directory.EnumerateFiles(Path.Combine(repository, "experiments/EarnedSupport"), "*.cs"))
            .Append(Path.Combine(repository, "experiments/EarnedSupport/EarnedSupport.csproj"))
            .Concat(Directory.EnumerateFiles(Path.Combine(repository, "Gojo"), "*.cs", SearchOption.AllDirectories)
                .Where(p => !Path.GetRelativePath(repository, p).Split(Path.DirectorySeparatorChar).Any(s => s is "obj" or "bin")));
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var path in sources.Distinct().Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(repository, path).Replace('\\', '/') + "\n"));
            hash.AppendData(File.ReadAllBytes(path));
        }
        hash.AppendData(File.ReadAllBytes(typeof(Program).Assembly.Location));
        hash.AppendData(File.ReadAllBytes(typeof(Carrier).Assembly.Location));
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private sealed record Options(string Directory, bool Resume, bool Campaign, string Scenario, long Seed,
        long Numerator, long Denominator, int Steps, int Window, int Checkpoint)
    {
        public static Options Parse(string[] args)
        {
            string? output = null, resume = null;
            string scenario = "periodic";
            long seed = 11, numerator = 1, denominator = 1;
            int steps = 1440, window = 120, checkpoint = 240;
            bool campaign = false;
            var supplied = new HashSet<string>();
            for (var i = 0; i < args.Length; i++)
            {
                var key = args[i];
                if (!supplied.Add(key)) throw new ArgumentException("Duplicate option: " + key);
                if (key == "--campaign") { campaign = true; continue; }
                if (++i >= args.Length) throw new ArgumentException("Missing value for " + key);
                var value = args[i];
                switch (key)
                {
                    case "--output": output = value; break;
                    case "--resume": resume = value; break;
                    case "--scenario": scenario = value; break;
                    case "--seed": seed = long.Parse(value); break;
                    case "--gain-numerator": numerator = long.Parse(value); break;
                    case "--gain-denominator": denominator = long.Parse(value); break;
                    case "--steps": steps = int.Parse(value); break;
                    case "--window": window = int.Parse(value); break;
                    case "--checkpoint": checkpoint = int.Parse(value); break;
                    default: throw new ArgumentException("Unknown option: " + key);
                }
            }
            if (output is not null && resume is not null) throw new ArgumentException("Choose output or resume.");
            if (resume is not null && supplied.Any(k => k is not "--resume" and not "--steps"))
                throw new ArgumentException("Resume keeps all frozen run settings; supply only the additional --steps.");
            if (campaign && supplied.Any(k => k is not "--campaign" and not "--output"))
                throw new ArgumentException("The frozen campaign accepts only --campaign and --output.");
            if (scenario is not "periodic" and not "flat-return" and not "random-sign" ||
                steps is <= 0 or > 1_000_000 || window <= 0 || checkpoint <= 0 || numerator < 0 || denominator <= 0)
                throw new ArgumentException("Invalid scenario, step count, reporting interval, or reward calibration.");
            return new(Path.GetFullPath(resume ?? output ?? "experiments/EarnedSupport/obj/local-run"),
                resume is not null, campaign, scenario, seed, numerator, denominator, steps, window, checkpoint);
        }
    }
}
