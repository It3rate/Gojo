using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using M = NegotiatedPorts.MarketProbe;

namespace MarketTraining;

internal static class Program
{
    private static bool stop;

    private static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--help"))
            {
                Console.WriteLine("MarketTraining --output <new-directory> [--seed 1] [--steps 240] [--window 40] [--checkpoint 120]\n" +
                    "MarketTraining --resume <directory> [--steps 240 additional arrivals]\n" +
                    "MarketTraining --checks-only\nMarketTraining --utility-probe --output <new-directory>");
                return 0;
            }
            if (args.SequenceEqual(new[] { "--checks-only" })) { Console.WriteLine($"Offline checks: {Checks.Run()} passed."); return 0; }
            var options = Options.Parse(args);
            if (options.UtilityProbe)
            {
                RequireEmpty(options.Directory);
                UtilityProbe.Run(options.Directory);
                Console.WriteLine("Utility probe results: " + Path.Combine(options.Directory, "utility-probes.json"));
                return 0;
            }
            Run(options);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void Run(Options options)
    {
        var repository = Checkpoint.Repository();
        var fingerprint = Checkpoint.SourceFingerprint(repository);
        var protocol = Checkpoint.Hash(File.ReadAllBytes(Path.Combine(repository, "experiments/MarketTraining/PROTOCOL.md")));
        var manifestPath = Path.Combine(options.Directory, "manifest.json");
        Directory.CreateDirectory(options.Directory);
        using var writerLock = new FileStream(Path.Combine(options.Directory, ".writer.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (!options.Resume && Directory.EnumerateFileSystemEntries(options.Directory)
            .Any(path => Path.GetFileName(path) != ".writer.lock"))
            throw new InvalidOperationException("Output directory is not empty; choose another directory or explicitly resume.");
        Manifest manifest;
        Relation root;
        EnvironmentSource environment;
        WindowTotals totals;
        long checkpointCount = 0;
        long committedBytes = 0;
        byte[] manifestBytes;
        if (options.Resume)
        {
            manifestBytes = File.ReadAllBytes(manifestPath);
            manifest = JsonSerializer.Deserialize<Manifest>(manifestBytes, Checkpoint.Json)!;
            if (manifest.SourceFingerprint != fingerprint || manifest.ProtocolFingerprint != protocol ||
                manifest.Generator != EnvironmentSource.Version || manifest.Format != "market-campaign-v1")
                throw new InvalidDataException("Resume requires the same source/build, protocol, generator and checkpoint format.");
            (root, var state) = Checkpoint.Load(options.Directory, Checkpoint.Hash(manifestBytes));
            environment = state.Environment;
            totals = state.Totals;
            checkpointCount = state.Checkpoints;
            committedBytes = state.WindowFileBytes;
            if (environment.Seed != manifest.Seed) throw new InvalidDataException("Source seed differs from run manifest.");
        }
        else
        {
            manifest = new("market-campaign-v1", EnvironmentSource.Version, "NegotiatedPorts-caf8cae-unchanged",
                options.Seed, options.Window, options.Checkpoint, fingerprint, protocol, DateTimeOffset.UtcNow);
            manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, Checkpoint.Json);
            File.WriteAllBytes(manifestPath, manifestBytes);
            root = Observation.ReleaseFrame(M.Create());
            environment = new() { Seed = options.Seed };
            totals = new();
        }
        var manifestHash = Checkpoint.Hash(manifestBytes);
        var window = new WindowTotals();
        var target = checked(environment.Position + options.Steps);
        var windowsPath = Path.Combine(options.Directory, "windows.jsonl");
        using var windows = new FileStream(windowsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        if (windows.Length < committedBytes) throw new InvalidDataException("Metrics file is shorter than the committed checkpoint offset.");
        if (windows.Length != committedBytes)
            Console.WriteLine($"Discarding {windows.Length - committedBytes} uncommitted metric bytes before deterministic replay.");
        windows.SetLength(committedBytes);
        windows.Position = committedBytes;
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop = true; };
        Console.CancelKeyPress += cancel;
        try
        {
            SaveCheckpoint(); // A new run is resumable even before its first admission.
            while (environment.Position < target && !stop)
            {
                var sample = environment.Next();
                var startAllocated = GC.GetTotalAllocatedBytes(precise: true);
                var started = Stopwatch.GetTimestamp();
                if (!M.TryObserve(root, sample.Input, true, true, 5, 8, out var next, out var frame))
                    throw new InvalidDataException("Current native admission was rejected.");
                var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var allocated = GC.GetTotalAllocatedBytes(precise: true) - startAllocated;
                root = next;
                started = Stopwatch.GetTimestamp();
                startAllocated = GC.GetTotalAllocatedBytes(precise: true);
                Observation.Add(window, root, frame, sample.Phase);
                Observation.Add(totals, root, frame, sample.Phase);
                root = Observation.ReleaseFrame(root);
                var observedElapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var observedAllocated = GC.GetTotalAllocatedBytes(precise: true) - startAllocated;
                foreach (var aggregate in new[] { window, totals })
                {
                    aggregate.NativeWallMilliseconds += elapsed;
                    aggregate.NativeAllocationBytes += allocated;
                    aggregate.ObserverWallMilliseconds += observedElapsed;
                    aggregate.ObserverAllocationBytes += observedAllocated;
                }
                if (!window.Conserved) throw new InvalidDataException("Native resource conservation failed; stopping without treating it as a score.");
                if (environment.Position % manifest.Window == 0) FlushWindow("window");
                if (environment.Position % manifest.Checkpoint == 0) { FlushWindow("checkpoint"); SaveCheckpoint(); }
            }
            FlushWindow(stop ? "interrupted" : "requested-end");
            SaveCheckpoint();
            File.WriteAllText(Path.Combine(options.Directory, "summary.json"), JsonSerializer.Serialize(new
            {
                ManifestHash = manifestHash, Reason = stop ? "interrupted" : "requested-end", environment.Position,
                Totals = totals, Current = Observation.State(root), Checkpoints = checkpointCount,
                Limit = "Fixed native market; no growth, no hidden refill. Quiet after depletion is not stability."
            }, Checkpoint.Json));
            Console.WriteLine($"{environment.Position} total arrivals; reservoir {M.Reservoir(root).Value}, spent {M.Spent(root).Value}; " +
                $"quiet {totals.QuietObservations}, depleted observations {totals.ReservoirEmptyObservations}. {options.Directory}");
        }
        finally { Console.CancelKeyPress -= cancel; }

        void FlushWindow(string reason)
        {
            if (window.Admissions == 0) return;
            var encoded = JsonSerializer.SerializeToUtf8Bytes(new { Reason = reason, Window = window, Current = Observation.State(root) }, Checkpoint.Compact);
            windows.Write(encoded); windows.WriteByte((byte)'\n'); windows.Flush(flushToDisk: true);
            window = new();
        }
        void SaveCheckpoint()
        {
            checkpointCount++;
            var startAllocated = GC.GetTotalAllocatedBytes(precise: true);
            var started = Stopwatch.GetTimestamp();
            var graph = GojoEncodedSnapshot.CaptureGraph(root);
            var graphCounts = new { Carriers = graph.Carriers.Length, Relations = graph.Relations.Length,
                TickReferences = graph.TickRefs.Length, graph.Placeholders.Length };
            Checkpoint.Save(options.Directory, root, new(manifestHash, environment, windows.Position, totals, checkpointCount));
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var allocated = GC.GetTotalAllocatedBytes(precise: true) - startAllocated;
            File.WriteAllText(Path.Combine(options.Directory, "latest-checkpoint-inspection.json"), JsonSerializer.Serialize(new
            {
                environment.Position, Counts = graphCounts, CheckpointWallMilliseconds = elapsed,
                CheckpointAllocationBytes = allocated, Bytes = new FileInfo(Path.Combine(options.Directory, "latest.checkpoint.zip")).Length,
                ResourceState = Observation.State(root)
            }, Checkpoint.Json));
        }
    }

    private static void RequireEmpty(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new InvalidOperationException("Output directory is not empty; select a new directory or explicitly --resume it.");
        Directory.CreateDirectory(directory);
    }

    private sealed record Options(string Directory, bool Resume, bool UtilityProbe, long Seed, int Steps, int Window, int Checkpoint)
    {
        public static Options Parse(string[] args)
        {
            string? output = null, resume = null;
            long seed = 1;
            var steps = 240; var window = 40; var checkpoint = 120; var probe = false;
            var supplied = new HashSet<string>();
            for (var index = 0; index < args.Length; index++)
            {
                var key = args[index];
                if (!supplied.Add(key)) throw new ArgumentException("Duplicate option: " + key);
                if (key == "--utility-probe") { probe = true; continue; }
                if (++index >= args.Length) throw new ArgumentException("Missing value for " + key);
                var value = args[index];
                switch (key)
                {
                    case "--output": output = value; break;
                    case "--resume": resume = value; break;
                    case "--seed": seed = long.Parse(value); break;
                    case "--steps": steps = int.Parse(value); break;
                    case "--window": window = int.Parse(value); break;
                    case "--checkpoint": checkpoint = int.Parse(value); break;
                    default: throw new ArgumentException("Unknown option: " + key);
                }
            }
            if (output is not null && resume is not null) throw new ArgumentException("Choose output or resume.");
            if (resume is not null && (supplied.Contains("--seed") || supplied.Contains("--window") || supplied.Contains("--checkpoint") || probe))
                throw new ArgumentException("Resume keeps the frozen seed/window/checkpoint settings.");
            if (steps <= 0 || steps > 1_000_000 || window <= 0 || checkpoint <= 0)
                throw new ArgumentException("Steps must be 1..1000000; window/checkpoint must be positive.");
            return new(Path.GetFullPath(resume ?? output ?? "experiments/MarketTraining/obj/local-run"), resume is not null, probe,
                seed, steps, window, checkpoint);
        }
    }
}
