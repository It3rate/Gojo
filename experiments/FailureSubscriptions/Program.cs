using System.Diagnostics;
using System.Text.Json;
using Gojo.Core;
using S = FailureSubscriptions.SubscriptionNetwork;

namespace FailureSubscriptions;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.SequenceEqual(new[] { "--checks-only" }))
            {
                Console.WriteLine($"{ProspectiveOfferChecks.Run()} offer and {Checks.Run()} subscription check groups passed.");
                return 0;
            }
            var options = Options.Parse(args);
            if (!options.Campaign) { Run(options); return 0; }
            RequireEmpty(options.Directory);
            foreach (var (seed, rotation) in new[] { (11L, 0), (47L, 1) })
            foreach (var scenario in new[] { "mixed", "wandering", "unpredictable" })
            foreach (var (horizon, prospects) in new[] { (0, false), (1, false), (5, false), (5, true) })
                if (!Run(options with { Campaign = false, Seed = seed, Rotation = rotation,
                    Scenario = scenario, Horizon = horizon, Prospects = prospects,
                    Directory = Path.Combine(options.Directory, $"{scenario}-{seed}-h{horizon}-p{(prospects ? 1 : 0)}") })) return 0;
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static bool Run(Options options)
    {
        if (!options.Resume) RequireEmpty(options.Directory);
        Directory.CreateDirectory(options.Directory);
        using var writerLock = new FileStream(Path.Combine(options.Directory, ".writer.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var repository = RunStore.Repository();
        var sourceHash = RunStore.Fingerprint(repository);
        var protocolHash = RunStore.Hash(File.ReadAllBytes(Path.Combine(repository, "experiments/FailureSubscriptions/PROTOCOL.md")));
        var manifestPath = Path.Combine(options.Directory, "manifest.json");
        string manifestHash;
        Relation root;
        EnvironmentSource source;
        ReportTotals totals;
        long offset = 0;
        if (options.Resume)
        {
            var bytes = File.ReadAllBytes(manifestPath);
            manifestHash = RunStore.Hash(bytes);
            var manifest = JsonSerializer.Deserialize<Manifest>(bytes, RunStore.Json)!;
            if (manifest.Format != "failure-subscriptions-v1" || manifest.SourceFingerprint != sourceHash || manifest.ProtocolFingerprint != protocolHash)
                throw new InvalidDataException("Resume needs the original source, build and protocol.");
            var restored = RunStore.Load(options.Directory, manifestHash);
            root = restored.Root; source = restored.State.Environment; totals = restored.State.Totals; offset = restored.State.WindowBytes;
            if (S.Version(root) != source.Position || ((CarrierLocation)S.Parts(root)[4]).Ticks != source.Position ||
                source.Seed != manifest.Seed || source.Scenario != manifest.Scenario || !S.Pass(S.Conservation(root)))
                throw new InvalidDataException("Restored native world and run state disagree.");
        }
        else
        {
            var manifest = new Manifest("failure-subscriptions-v1", options.Seed, options.Scenario,
                options.Horizon, options.Prospects, options.Rotation, sourceHash, protocolHash);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, RunStore.Json);
            File.WriteAllBytes(manifestPath, bytes); manifestHash = RunStore.Hash(bytes);
            root = S.RetainContinuation(S.Create(options.Horizon, options.Prospects, options.Rotation));
            source = new() { Seed = options.Seed, Scenario = options.Scenario };
            totals = new();
        }
        using var windows = new FileStream(Path.Combine(options.Directory, "windows.jsonl"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        if (windows.Length < offset) throw new InvalidDataException("Report is shorter than its checkpoint; refusing to pad it.");
        windows.SetLength(offset); windows.Position = offset;
        var window = new ReportTotals();
        string? lastPhase = null;
        var stop = false;
        ConsoleCancelEventHandler cancel = (_, args) => { args.Cancel = true; stop = true; };
        Console.CancelKeyPress += cancel;
        try
        {
            for (var step = 0; step < options.Steps && !stop; step++)
            {
                var sample = source.Next();
                if (lastPhase is not null && lastPhase != sample.Phase) Flush("phase-change");
                lastPhase = sample.Phase;
                var allocated = GC.GetTotalAllocatedBytes(true); var started = Stopwatch.GetTimestamp();
                if (!S.TryObserve(root, sample.Input, out var next, out var frame))
                    throw new InvalidDataException("Native observation rejected a current supplied sample.");
                root = next;
                var nativeTime = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var nativeAllocation = GC.GetTotalAllocatedBytes(true) - allocated;
                allocated = GC.GetTotalAllocatedBytes(true); started = Stopwatch.GetTimestamp();
                Report.Add(window, root, frame, sample.Phase);
                Report.Add(totals, root, frame, sample.Phase);
                root = S.RetainContinuation(root);
                var reportTime = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var reportAllocation = GC.GetTotalAllocatedBytes(true) - allocated;
                foreach (var aggregate in new[] { window, totals })
                {
                    aggregate.NativeWallMilliseconds += nativeTime; aggregate.NativeAllocationBytes += nativeAllocation;
                    aggregate.ReportWallMilliseconds += reportTime; aggregate.ReportAllocationBytes += reportAllocation;
                }
                if (!S.Pass(S.Conservation(root))) throw new InvalidDataException("Native currency ledger failed.");
                if (source.Position % 48 == 0) Flush("window");
                if (source.Position % 80 == 0) { Flush("checkpoint"); Save(); }
            }
            Flush(stop ? "interrupted" : "requested-end"); Save();
            File.WriteAllText(Path.Combine(options.Directory, "summary.json"), JsonSerializer.Serialize(new
            {
                ManifestHash = manifestHash, Reason = stop ? "interrupted" : "requested-end", source.Position,
                Totals = totals, Current = Report.State(root),
                Limit = "Supplied recipes, context partitions, costs and attention laws; future estimates are conditional empirical reads, not guaranteed correctness."
            }, RunStore.Json));
            Console.WriteLine($"{Path.GetFileName(options.Directory)}: {source.Position} observations completed.");
        }
        finally { Console.CancelKeyPress -= cancel; }
        return !stop;

        void Flush(string reason)
        {
            if (window.Admissions == 0) return;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { Reason = reason, Window = window, Current = Report.State(root) });
            windows.Write(bytes); windows.WriteByte((byte)'\n'); windows.Flush(true); window = new();
        }
        void Save()
        {
            var allocated = GC.GetTotalAllocatedBytes(true); var started = Stopwatch.GetTimestamp();
            RunStore.Save(options.Directory, root, new(manifestHash, source, windows.Position, totals));
            var graph = GojoEncodedSnapshot.CaptureGraph(root);
            File.WriteAllText(Path.Combine(options.Directory, "latest-checkpoint-inspection.json"), JsonSerializer.Serialize(new
            {
                source.Position, Carriers = graph.Carriers.Length, Relations = graph.Relations.Length,
                Bytes = new FileInfo(Path.Combine(options.Directory, "latest.checkpoint.zip")).Length,
                WallMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                AllocationBytes = GC.GetTotalAllocatedBytes(true) - allocated
            }, RunStore.Json));
        }
    }

    private static void RequireEmpty(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new InvalidOperationException("Output must be empty; select a new directory or --resume.");
        Directory.CreateDirectory(directory);
    }

    private sealed record Options(string Directory, bool Resume, bool Campaign, long Seed, string Scenario,
        int Horizon, bool Prospects, int Rotation, int Steps)
    {
        public static Options Parse(string[] args)
        {
            string? output = null, resume = null;
            long seed = 11; string scenario = "mixed"; bool prospects = true, campaign = false;
            int horizon = 5, rotation = 0, steps = 241;
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
                    case "--seed": seed = long.Parse(value); break;
                    case "--scenario": scenario = value; break;
                    case "--horizon": horizon = int.Parse(value); break;
                    case "--prospects": prospects = value switch { "0" => false, "1" => true, _ => throw new ArgumentException("Use 0 or 1 for prospects.") }; break;
                    case "--rotation": rotation = int.Parse(value); break;
                    case "--steps": steps = int.Parse(value); break;
                    default: throw new ArgumentException("Unknown option: " + key);
                }
            }
            if (output is not null && resume is not null) throw new ArgumentException("Choose output or resume.");
            if (campaign && supplied.Any(k => k is not "--campaign" and not "--output"))
                throw new ArgumentException("Frozen campaign accepts only --campaign and --output.");
            if (resume is not null && supplied.Any(k => k is not "--resume" and not "--steps"))
                throw new ArgumentException("Resume keeps frozen settings; only --steps can change.");
            if (scenario is not "mixed" and not "wandering" and not "unpredictable" || horizon is not 0 and not 1 and not 5 ||
                rotation is < 0 or > 3 || steps is <= 0 or > 1_000_000)
                throw new ArgumentException("Invalid scenario, horizon, rotation or step count.");
            return new(Path.GetFullPath(resume ?? output ?? "experiments/FailureSubscriptions/obj/local-run"),
                resume is not null, campaign, seed, scenario, horizon, prospects, rotation, steps);
        }
    }
}
